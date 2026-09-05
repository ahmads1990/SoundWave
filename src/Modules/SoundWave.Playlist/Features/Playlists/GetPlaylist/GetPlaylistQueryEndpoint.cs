using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.GetPlaylist;

/// <summary>
/// Exposes the HTTP endpoint for retrieving full details of a single playlist.
/// </summary>
internal class GetPlaylistQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/playlists/{playlistId:guid}", Handle)
            .AllowAnonymous()
            .WithTags(Constants.Tags.Playlists)
            .WithSummary("Get playlist by ID")
            .WithDescription("Retrieves full playlist metadata and ordered tracklist. Private playlists require owner or collaborator authentication.")
            .Produces<SuccessResponse<PlaylistDetailDto>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PlaylistDetailDto>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid playlistId,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = new GetPlaylistQuery(playlistId);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PlaylistDetailDto>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.PlaylistNotFound => Results.NotFound(response),
                _                              => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<PlaylistDetailDto>(result.Data!));
    }
}
