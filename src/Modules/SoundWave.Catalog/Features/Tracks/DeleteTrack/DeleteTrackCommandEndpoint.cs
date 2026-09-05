using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Tracks.DeleteTrack;

/// <summary>
/// Exposes the HTTP endpoint for deleting a track from an album.
/// Restricted to the primary artist of the parent album.
/// </summary>
internal class DeleteTrackCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("v1/catalog/tracks/{trackId:guid}", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .WithTags(Constants.Tags.Tracks)
            .WithSummary("Delete a track")
            .WithDescription("Soft-deletes a track from its album, decrements album track count, and re-sequences remaining track numbers.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid trackId,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new DeleteTrackCommand(trackId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.TrackNotFound           => Results.NotFound(response),
                CatalogError.UnauthorizedTrackAccess => Results.Forbid(),
                CatalogError.UserNotAuthenticated    => Results.Unauthorized(),
                _                                    => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Guid>(result.Data));
    }
}
