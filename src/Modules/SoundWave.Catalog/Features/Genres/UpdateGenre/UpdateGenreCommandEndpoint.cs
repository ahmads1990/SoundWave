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

namespace SoundWave.Catalog.Features.Genres.UpdateGenre;

/// <summary>
/// Exposes the HTTP endpoint for updating an existing music genre or mood in the catalog.
/// Restricted to administrators only.
/// </summary>
internal class UpdateGenreCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("v1/catalog/genres/{id:int}", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()))
            .AddEndpointFilter<ValidationFilter<UpdateGenreRequest>>()
            .WithTags(Constants.Tags.Genres)
            .WithSummary("Update an existing genre or mood")
            .WithDescription("Updates an existing genre or mood category in the catalog. Restricted to administrators.")
            .Produces<SuccessResponse<int>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<int>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<int>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<int>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<int>>(StatusCodes.Status404NotFound)
            .Produces<FailureResponse<int>>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        int id,
        UpdateGenreRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new UpdateGenreCommand(id, request.Name, request.Type);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<int>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.GenreNotFound => Results.NotFound(response),
                CatalogError.GenreAlreadyExists => Results.Conflict(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<int>(result.Data));
    }
}
