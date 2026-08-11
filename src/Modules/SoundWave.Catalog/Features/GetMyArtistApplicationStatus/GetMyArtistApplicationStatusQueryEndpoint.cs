using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.GetMyArtistApplicationStatus;

/// <summary>
/// Exposes the HTTP endpoint for the logged-in user to check their artist application status.
/// </summary>
internal class GetMyArtistApplicationStatusQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/artists/applications/my", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get my artist application status")
            .WithDescription("Retrieves the status and review details of the authenticated user's artist account application.")
            .Produces<SuccessResponse<ArtistApplicationStatusDto>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<ArtistApplicationStatusDto>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<ArtistApplicationStatusDto>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyArtistApplicationStatusQuery();
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<ArtistApplicationStatusDto>(result.Error.ToApiErrorCode(), result.ErrorMessage);

            return result.Error switch
            {
                CatalogError.ArtistApplicationNotFound => Results.NotFound(response),
                CatalogError.UserNotAuthenticated     => Results.Unauthorized(),
                _                                     => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<ArtistApplicationStatusDto>(result.Data!));
    }
}

