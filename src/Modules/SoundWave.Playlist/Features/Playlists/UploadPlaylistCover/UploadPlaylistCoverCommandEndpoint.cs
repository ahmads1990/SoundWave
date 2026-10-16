using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.UploadPlaylistCover;

/// <summary>
/// Exposes the HTTP endpoint for uploading or updating a playlist's cover artwork.
/// Restricted to the playlist's creator/owner.
/// </summary>
internal class UploadPlaylistCoverCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/playlists/{playlistId:guid}/cover", Handle)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags(Constants.Tags.Playlists)
            .WithSummary("Upload playlist cover artwork")
            .WithDescription("Uploads an image file (.jpg, .png, .webp) up to 5 MB as the playlist's cover artwork.")
            .Produces<SuccessResponse<UploadImageResponse>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid playlistId,
        IFormFile? file,
        ISender sender,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new FailureResponse<UploadImageResponse>(
                ApiErrorCode.ValidationFailed, "An image file must be provided."));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadPlaylistCoverCommand(playlistId, stream, file.FileName, file.Length);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<UploadImageResponse>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.PlaylistNotFound => Results.NotFound(response),
                PlaylistError.Unauthorized => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                PlaylistError.SystemPlaylistProtected => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                PlaylistError.UserNotAuthenticated => Results.Unauthorized(),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<UploadImageResponse>(result.Data!));
    }
}
