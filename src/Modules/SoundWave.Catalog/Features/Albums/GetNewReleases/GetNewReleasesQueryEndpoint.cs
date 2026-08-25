using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.GetNewReleases;

/// <summary>
/// Exposes the HTTP endpoint for retrieving newly released published albums from the catalog.
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
            .WithSummary("Get new album releases")
            .WithDescription($"Retrieves a paginated list of recently published albums with optional filtering by genre, album type, and maximum age in days. Allowed album types: {EnumHelper.ToAllowedValuesString<AlbumType>()}. Ordered by {nameof(Album.ReleaseDate)} descending.")
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
