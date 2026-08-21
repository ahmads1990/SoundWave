using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Artists.GetArtistProfile;

/// <summary>
/// Exposes the HTTP endpoint for retrieving an artist's profile, top tracks, and published albums.
/// </summary>
internal class GetArtistProfileQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/artists/{id:guid}", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get artist profile")
            .WithDescription("Retrieves the full profile, top tracks, and published discography of an artist.")
            .Produces<SuccessResponse<ArtistProfileDto>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<ArtistProfileDto>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<ArtistProfileDto>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<ArtistProfileDto>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken = default)
    {
        var query = new GetArtistProfileQuery(id);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<ArtistProfileDto>(result.Error.ToApiErrorCode(), result.ErrorMessage);

            return result.Error switch
            {
                CatalogError.ArtistNotFound => Results.NotFound(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<ArtistProfileDto>(result.Data!));
    }
}
