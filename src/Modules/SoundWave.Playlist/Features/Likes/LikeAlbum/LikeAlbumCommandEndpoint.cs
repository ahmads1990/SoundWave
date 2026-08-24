using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Likes.LikeAlbum;

/// <summary>
/// Exposes the HTTP endpoint for saving/liking an album.
/// </summary>
internal class LikeAlbumCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/albums/{albumId:guid}/like", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Save an album to library")
            .WithDescription("Adds an album to the user's saved albums in their library.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        Guid albumId,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new LikeAlbumCommand(albumId);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.UserNotAuthenticated => Results.Unauthorized(),
                _                                  => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }
}
