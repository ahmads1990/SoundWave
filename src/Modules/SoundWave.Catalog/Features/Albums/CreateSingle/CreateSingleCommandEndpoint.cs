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

namespace SoundWave.Catalog.Features.Albums.CreateSingle;

/// <summary>
/// Exposes the HTTP endpoint for creating a 1-step single release (Album + Track).
/// Restricted to artists only.
/// </summary>
internal class CreateSingleCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/catalog/singles", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<CreateSingleRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Create a 1-step single release")
            .WithDescription("Atomically creates a single release (Album of type Single + Track #1) for the authenticated artist.")
            .Produces<SuccessResponse<CreateSingleResponse>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<CreateSingleResponse>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<CreateSingleResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<CreateSingleResponse>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<CreateSingleResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        CreateSingleRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = request.Adapt<CreateSingleCommand>();
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<CreateSingleResponse>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.ArtistNotFound       => Results.NotFound(response),
                CatalogError.InvalidGenreId       => Results.BadRequest(response),
                CatalogError.UserNotAuthenticated => Results.Unauthorized(),
                _                                 => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/catalog/singles/{result.Data!.AlbumId}", new SuccessResponse<CreateSingleResponse>(result.Data));
    }
}
