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

namespace SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;

/// <summary>
/// Exposes the HTTP endpoint for generating a presigned S3 PUT URL for uploading track audio.
/// Restricted to artists who own the album containing the track.
/// </summary>
internal class GetTrackUploadUrlCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/catalog/tracks/{trackId:guid}/audio/upload-url", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<GetTrackUploadUrlRequest>>()
            .WithTags(Constants.Tags.Tracks)
            .WithSummary("Get track audio upload URL via presigned S3 URL")
            .WithDescription("Generates a presigned S3 PUT URL allowing direct streaming upload to object storage (max 50 MB, mp3/flac/wav/aac/m4a).")
            .Produces<SuccessResponse<TrackUploadUrlResponse>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<TrackUploadUrlResponse>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<TrackUploadUrlResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<TrackUploadUrlResponse>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<TrackUploadUrlResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid trackId,
        [FromBody] GetTrackUploadUrlRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new GetTrackUploadUrlCommand(
            trackId,
            request.FileName,
            request.FileSizeBytes,
            request.ContentType);

        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<TrackUploadUrlResponse>(
                result.Error.ToApiErrorCode(),
                result.ErrorMessage);

            return result.Error switch
            {
                CatalogError.TrackNotFound           => Results.NotFound(response),
                CatalogError.UnauthorizedTrackAccess => Results.Json(response, statusCode: StatusCodes.Status403Forbidden),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<TrackUploadUrlResponse>(result.Data!));
    }
}
