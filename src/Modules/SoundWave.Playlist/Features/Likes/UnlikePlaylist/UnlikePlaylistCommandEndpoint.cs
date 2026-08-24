using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Likes.UnlikePlaylist;

/// <summary>
/// Exposes the HTTP endpoint for unfollowing/unliking a playlist.
/// </summary>
internal class UnlikePlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/playlists/{playlistId:guid}/like", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Unlike/unfollow a playlist")
            .WithDescription("Removes a playlist from the user's library and decrements its follower count.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        Guid playlistId,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new UnlikePlaylistCommand(playlistId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.UserNotAuthenticated => Results.Unauthorized(),
                _                                  => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }
}
