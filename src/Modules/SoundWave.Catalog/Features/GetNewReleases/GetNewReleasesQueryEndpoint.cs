using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.GetNewReleases;

/// <summary>
/// Exposes the HTTP endpoint for retrieving paginated new album releases with optional filters.
/// Public endpoint — no authentication required.
/// </summary>
internal class GetNewReleasesQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/albums/new-releases", Handle)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<GetNewReleasesRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get new releases")
            .WithDescription("Returns a paginated list of the most recently released published albums with optional filtering by genre, album type, and days old.")
            .Produces<SuccessResponse<PaginatedResponse<AlbumSummaryDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PaginatedResponse<AlbumSummaryDto>>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetNewReleasesRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = request.Adapt<GetNewReleasesQuery>();
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PaginatedResponse<AlbumSummaryDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<PaginatedResponse<AlbumSummaryDto>>(result.Data!));
    }
}
