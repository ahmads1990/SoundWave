using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.PublishAlbum;

/// <summary>
/// Exposes the HTTP endpoint for publishing an album.
/// Restricted to artists only.
/// </summary>
internal class PublishAlbumCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/catalog/albums/{albumId:guid}/publish", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .WithTags(Constants.Tags.Albums)
            .WithSummary("Publish an album")
            .WithDescription("Publishes an album, making it publicly visible to listeners. The album must have at least one track.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid albumId,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new PublishAlbumCommand(albumId), ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.AlbumNotFound           => Results.NotFound(response),
                CatalogError.ArtistNotFound          => Results.NotFound(response),
                CatalogError.AlbumAlreadyPublished   => Results.Conflict(response),
                CatalogError.CannotPublishEmptyAlbum => Results.BadRequest(response),
                CatalogError.UnauthorizedAlbumAccess => Results.Forbid(),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Guid>(result.Data));
    }
}
