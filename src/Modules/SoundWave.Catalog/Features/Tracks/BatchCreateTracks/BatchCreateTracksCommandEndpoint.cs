using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Tracks.BatchCreateTracks;

/// <summary>
/// Exposes the HTTP endpoint for batch creating tracks within an album
/// and generating presigned S3 PUT URLs for uploading raw audio directly to object storage.
/// Restricted to artists who own the album.
/// </summary>
internal class BatchCreateTracksCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/catalog/albums/{albumId:guid}/tracks/batch", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<BatchCreateTracksRequest>>()
            .WithTags(Constants.Tags.Tracks)
            .WithSummary("Batch create tracks with presigned audio upload URLs")
            .WithDescription("Creates multiple tracks for an unpublished album in a single transaction and generates direct-to-S3 presigned upload URLs for each track's audio file.")
            .Produces<SuccessResponse<BatchCreateTracksResponse>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<BatchCreateTracksResponse>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<BatchCreateTracksResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<BatchCreateTracksResponse>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<BatchCreateTracksResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid albumId,
        [FromBody] BatchCreateTracksRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new BatchCreateTracksCommand(
            albumId,
            request.Tracks.Select(t => new BatchTrackItem(
                t.Title,
                t.FileName,
                t.FileSizeBytes,
                t.ContentType,
                t.GenreIds,
                t.FeaturedArtistIds)).ToList());

        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<BatchCreateTracksResponse>(
                result.Error.ToApiErrorCode(),
                result.ErrorMessage);

            return result.Error switch
            {
                CatalogError.AlbumNotFound           => Results.NotFound(response),
                CatalogError.ArtistNotFound          => Results.NotFound(response),
                CatalogError.UnauthorizedAlbumAccess => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                CatalogError.AlbumAlreadyPublished   => Results.BadRequest(response),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Created(
            $"/api/v1/catalog/albums/{albumId}",
            new SuccessResponse<BatchCreateTracksResponse>(result.Data!));
    }
}
