using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Tracks.GetTrackStatus;

/// <summary>
/// Exposes the HTTP endpoint for retrieving the processing status of a track audio file.
/// </summary>
internal class GetTrackStatusEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/catalog/tracks/{trackId:guid}/status", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Tracks)
            .WithSummary("Get track audio processing status")
            .WithDescription("Retrieves the current audio file state (PendingUpload, PendingValidation, Processing, Available, Failed — null when no audio has been uploaded), error logs if any, and HLS playlist paths.")
            .Produces<SuccessResponse<TrackStatusResponse>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<TrackStatusResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<TrackStatusResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid trackId,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = new GetTrackStatusQuery(trackId);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<TrackStatusResponse>(
                result.Error.ToApiErrorCode(),
                result.ErrorMessage);

            return result.Error switch
            {
                CatalogError.TrackNotFound => Results.NotFound(response),
                _                          => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<TrackStatusResponse>(result.Data!));
    }
}
