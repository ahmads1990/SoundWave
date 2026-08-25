using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.Playlist.Features.Playlists.GetPlaylist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.GetLikedSongsPlaylist;

/// <summary>
/// Exposes the HTTP endpoint for retrieving the authenticated user's system "Liked Songs" playlist.
/// </summary>
internal class GetLikedSongsPlaylistQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/playlists/liked-songs", Handle)
            .RequireAuthorization()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get Liked Songs playlist")
            .WithDescription("Retrieves the authenticated user's system 'Liked Songs' playlist with tracks ordered by position.")
            .Produces<SuccessResponse<PlaylistDetailDto>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PlaylistDetailDto>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct = default)
    {
        var query = new GetLikedSongsPlaylistQuery();
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PlaylistDetailDto>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<PlaylistDetailDto>(result.Data!));
    }
}
