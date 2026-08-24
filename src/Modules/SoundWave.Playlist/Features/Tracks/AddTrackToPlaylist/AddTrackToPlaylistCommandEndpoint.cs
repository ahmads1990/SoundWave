using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Tracks.AddTrackToPlaylist;

/// <summary>
/// Exposes the HTTP endpoint for adding a track to a playlist.
/// </summary>
internal class AddTrackToPlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/playlists/{playlistId:guid}/tracks", Handle)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<AddTrackToPlaylistRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Add track to playlist")
            .WithDescription("Appends a track to the end of a playlist owned by the authenticated user.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid playlistId,
        AddTrackToPlaylistRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new AddTrackToPlaylistCommand(playlistId, request.TrackId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.PlaylistNotFound        => Results.NotFound(response),
                PlaylistError.Unauthorized            => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                PlaylistError.SystemPlaylistProtected => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                PlaylistError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                     => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/playlists/{playlistId}/tracks", new SuccessResponse<Guid>(result.Data));
    }
}
