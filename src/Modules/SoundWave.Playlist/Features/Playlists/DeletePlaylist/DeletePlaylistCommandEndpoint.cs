using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.DeletePlaylist;

/// <summary>
/// Exposes the HTTP endpoint for deleting a playlist.
/// </summary>
internal class DeletePlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/playlists/{id:guid}", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Delete a playlist")
            .WithDescription("Soft-deletes a playlist owned by the authenticated caller. System playlists cannot be deleted.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<bool>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new DeletePlaylistCommand(id);
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
