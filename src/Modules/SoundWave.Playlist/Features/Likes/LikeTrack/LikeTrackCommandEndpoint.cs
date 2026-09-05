using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Likes.LikeTrack;

/// <summary>
/// Exposes the HTTP endpoint for liking a track.
/// </summary>
internal class LikeTrackCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/playlists/likes/tracks/{trackId:guid}", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Likes)
            .WithSummary("Like a track")
            .WithDescription("Adds a track to the user's liked tracks and syncs it to their system 'Liked Songs' playlist.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        Guid trackId,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new LikeTrackCommand(trackId);
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
