using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.EditAlbum;

/// <summary>
/// Exposes the HTTP endpoint for editing an existing album's metadata.
/// Restricted to artists only (specifically the album's primary artist).
/// </summary>
internal class EditAlbumCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("v1/catalog/albums/{albumId:guid}", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<EditAlbumRequest>>()
            .WithTags(Constants.Tags.Albums)
            .WithSummary("Edit album metadata")
            .WithDescription("Updates the title, album type, cover art, description, genres, and collaborating artists of an existing album.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid albumId,
        EditAlbumRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new EditAlbumCommand(
            albumId,
            request.Title,
            request.AlbumType,
            request.ReleaseDate,
            request.CoverImageUrl,
            request.Description,
            request.GenreIds,
            request.FeaturedArtistIds);

        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.AlbumNotFound           => Results.NotFound(response),
                CatalogError.UnauthorizedAlbumAccess => Results.Forbid(),
                CatalogError.InvalidGenreId          => Results.BadRequest(response),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Guid>(result.Data));
    }
}
