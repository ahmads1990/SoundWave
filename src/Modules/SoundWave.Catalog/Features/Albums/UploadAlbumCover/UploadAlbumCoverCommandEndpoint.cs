using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.UploadAlbumCover;

/// <summary>
/// Exposes the HTTP endpoint for uploading or updating an album's cover artwork.
/// Restricted to authenticated artists who are the primary artist of the album.
/// </summary>
internal class UploadAlbumCoverCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/catalog/albums/{albumId:guid}/cover", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .DisableAntiforgery()
            .WithTags(Constants.Tags.Albums)
            .WithSummary("Upload album cover artwork")
            .WithDescription("Uploads an image file (.jpg, .png, .webp) up to 5 MB as the album's cover image.")
            .Produces<SuccessResponse<UploadImageResponse>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid albumId,
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
        var command = new UploadAlbumCoverCommand(albumId, stream, file.FileName, file.Length);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<UploadImageResponse>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.AlbumNotFound => Results.NotFound(response),
                CatalogError.UnauthorizedAlbumAccess => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                CatalogError.UserNotAuthenticated => Results.Unauthorized(),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<UploadImageResponse>(result.Data!));
    }
}
