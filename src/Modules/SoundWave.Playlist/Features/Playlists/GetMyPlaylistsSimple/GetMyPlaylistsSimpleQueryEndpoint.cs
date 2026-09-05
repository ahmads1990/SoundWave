using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.GetMyPlaylistsSimple;

/// <summary>
/// Exposes the HTTP endpoint for retrieving the authenticated user's editable playlists.
/// </summary>
internal class GetMyPlaylistsSimpleQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/playlists/my/simple", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Playlists)
            .WithSummary("Get user's editable playlists (lightweight)")
            .WithDescription($"Retrieves a lightweight summary of playlists owned by the authenticated user for quick selection in modals, ordered by {nameof(PlaylistEntity.CreatedDate)} descending.")
            .Produces<SuccessResponse<IReadOnlyList<SimplePlaylistDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<IReadOnlyList<SimplePlaylistDto>>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        Guid? trackId,
        string? searchTerm,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = new GetMyPlaylistsSimpleQuery(trackId, searchTerm);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<IReadOnlyList<SimplePlaylistDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<IReadOnlyList<SimplePlaylistDto>>(result.Data!));
    }
}
