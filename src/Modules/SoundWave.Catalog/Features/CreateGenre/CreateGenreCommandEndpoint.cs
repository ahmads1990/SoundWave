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

namespace SoundWave.Catalog.Features.CreateGenre;

/// <summary>
/// Exposes the HTTP endpoint for creating a new music genre or mood in the catalog.
/// Restricted to administrators only.
/// </summary>
internal class CreateGenreCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/catalog/genres", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()))
            .AddEndpointFilter<ValidationFilter<CreateGenreRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Create a new genre or mood")
            .WithDescription("Creates a new genre or mood category in the catalog. Restricted to administrators.")
            .Produces<SuccessResponse<int>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<int>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<int>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<int>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<int>>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateGenreRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = request.Adapt<CreateGenreCommand>();
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<int>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.GenreAlreadyExists => Results.Conflict(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/catalog/genres/{result.Data}", new SuccessResponse<int>(result.Data));
    }
}
