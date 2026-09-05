using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Likes.LikePlaylist;

/// <summary>
/// Exposes the HTTP endpoint for following/liking a playlist.
/// </summary>
internal class LikePlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/playlists/likes/{playlistId:guid}", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Likes)
            .WithSummary("Like/follow a playlist")
            .WithDescription("Saves/follows a playlist to the user's library and increments its follower count.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid playlistId,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new LikePlaylistCommand(playlistId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.PlaylistNotFound     => Results.NotFound(response),
                PlaylistError.UserNotAuthenticated => Results.Unauthorized(),
                _                                  => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }
}
