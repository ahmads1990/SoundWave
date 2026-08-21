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

namespace SoundWave.Catalog.Features.Genres.ListGenres;

/// <summary>
/// Exposes the HTTP endpoint for retrieving paginated music genres and moods from the catalog.
/// </summary>
internal class ListGenresQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/genres", Handle)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ListGenresRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get paginated list of genres and moods")
            .WithDescription("Retrieves a paginated list of genres and moods with optional filtering and sorting.")
            .Produces<SuccessResponse<PaginatedResponse<ListGenreDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PaginatedResponse<ListGenreDto>>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<PaginatedResponse<ListGenreDto>>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        [AsParameters] ListGenresRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = request.Adapt<ListGenresQuery>();
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PaginatedResponse<ListGenreDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<PaginatedResponse<ListGenreDto>>(result.Data!));
    }
}
