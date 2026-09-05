using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.EditPlaylist;

/// <summary>
/// Exposes the HTTP endpoint for editing a playlist.
/// </summary>
internal class EditPlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("v1/playlists/{id:guid}", Handle)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<EditPlaylistRequest>>()
            .WithTags(Constants.Tags.Playlists)
            .WithSummary("Edit a playlist")
            .WithDescription("Updates the title, description, or visibility of an existing playlist owned by the caller.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<bool>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        EditPlaylistRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new EditPlaylistCommand(id, request.Title, request.Description, request.Visibility);
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
