using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.EditTrackMetadata;

/// <summary>
/// Exposes the HTTP endpoint for editing track metadata.
/// Restricted to artists only.
/// </summary>
internal class EditTrackMetadataCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/catalog/tracks/{trackId:guid}", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<EditTrackMetadataRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Edit track metadata")
            .WithDescription("Updates a track's title, duration, genres, and featured artists.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid trackId,
        EditTrackMetadataRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new EditTrackMetadataCommand(
            trackId,
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
                CatalogError.TrackNotFound           => Results.NotFound(response),
                CatalogError.ArtistNotFound          => Results.NotFound(response),
                CatalogError.UnauthorizedTrackAccess => Results.Forbid(),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Guid>(result.Data));
    }
}
