using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Tracks.RemoveTrackFromPlaylist;

/// <summary>
/// Exposes the HTTP endpoint for removing a track from a playlist.
/// </summary>
internal class RemoveTrackFromPlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/playlists/{playlistId:guid}/tracks/{trackId:guid}", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Remove track from playlist")
            .WithDescription("Removes a track from a playlist, re-gaps remaining positions, and decrements track count.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<bool>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid playlistId,
        Guid trackId,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new RemoveTrackFromPlaylistCommand(playlistId, trackId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.PlaylistNotFound        => Results.NotFound(response),
                PlaylistError.Unauthorized            => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                PlaylistError.SystemPlaylistProtected => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                PlaylistError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                     => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }
}
