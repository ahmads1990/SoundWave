using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.CreateTrack;

/// <summary>
/// Exposes the HTTP endpoint for creating a track within an album.
/// Restricted to artists only.
/// </summary>
internal class CreateTrackCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/catalog/albums/{albumId:guid}/tracks", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<CreateTrackRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Create a track in an album")
            .WithDescription("Creates a new track (metadata only) within an existing album. Audio upload happens separately in Phase 2.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid albumId,
        CreateTrackRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new CreateTrackCommand(
            albumId,
            request.Title,
            request.DurationSeconds,
            request.GenreIds,
            request.FeaturedArtistIds);

        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.AlbumNotFound            => Results.NotFound(response),
                CatalogError.ArtistNotFound           => Results.NotFound(response),
                CatalogError.UnauthorizedAlbumAccess  => Results.Forbid(),
                CatalogError.UserNotAuthenticated     => Results.Unauthorized(),
                _                                     => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/catalog/tracks/{result.Data}", new SuccessResponse<Guid>(result.Data));
    }
}
