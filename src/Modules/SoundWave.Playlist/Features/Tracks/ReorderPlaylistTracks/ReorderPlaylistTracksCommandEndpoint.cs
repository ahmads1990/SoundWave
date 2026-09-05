using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Tracks.ReorderPlaylistTracks;

/// <summary>
/// Exposes the HTTP endpoint for reordering a track within a playlist.
/// </summary>
internal class ReorderPlaylistTracksCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("v1/playlists/{playlistId:guid}/tracks/reorder", Handle)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ReorderPlaylistTracksRequest>>()
            .WithTags(Constants.Tags.PlaylistTracks)
            .WithSummary("Reorder playlist tracks")
            .WithDescription("Moves a track to a new 1-based position within the playlist, automatically shifting intermediate tracks.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<bool>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid playlistId,
        ReorderPlaylistTracksRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new ReorderPlaylistTracksCommand(playlistId, request.TrackId, request.NewPosition);
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
