using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Tracks.MoveTrackToAlbum;

/// <summary>
/// Exposes the HTTP endpoint for moving a track to a different album.
/// Restricted to artists who own both the source and target albums.
/// </summary>
internal class MoveTrackToAlbumCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("v1/catalog/tracks/{trackId:guid}/album", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<MoveTrackToAlbumRequest>>()
            .WithTags(Constants.Tags.Tracks)
            .WithSummary("Move track to another album")
            .WithDescription("Reassigns a track to another album owned by the same artist, preserving track stats and audio while updating track counts and re-sequencing track numbers in both albums.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid trackId,
        MoveTrackToAlbumRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new MoveTrackToAlbumCommand(trackId, request.TargetAlbumId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.TrackNotFound           => Results.NotFound(response),
                CatalogError.AlbumNotFound           => Results.NotFound(response),
                CatalogError.UnauthorizedTrackAccess => Results.Forbid(),
                CatalogError.UnauthorizedAlbumAccess => Results.Forbid(),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Guid>(result.Data));
    }
}
