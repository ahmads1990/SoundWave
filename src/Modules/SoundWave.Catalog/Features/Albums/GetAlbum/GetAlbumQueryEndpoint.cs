using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.GetAlbum;

/// <summary>
/// Exposes the HTTP endpoint for retrieving a single album with its tracklist.
/// Public endpoint — no authentication required.
/// </summary>
internal class GetAlbumQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/albums/{albumId:guid}", Handle)
            .AllowAnonymous()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get album details")
            .WithDescription("Returns a single album with its full ordered tracklist, artists, and genres.")
            .Produces<SuccessResponse<AlbumDetailsDto>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<AlbumDetailsDto>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid albumId,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAlbumQuery(albumId), ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<AlbumDetailsDto>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.AlbumNotFound => Results.NotFound(response),
                _                          => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<AlbumDetailsDto>(result.Data!));
    }
}
