using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.GetUserPublicPlaylists;

/// <summary>
/// Exposes the HTTP endpoint for retrieving public playlists created by a specific user.
/// </summary>
internal class GetUserPublicPlaylistsQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/users/{userId:guid}/playlists", Handle)
            .AllowAnonymous()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Get user's public playlists")
            .WithDescription($"Retrieves all public playlists created by a specific user or artist profile, ordered by {nameof(PlaylistEntity.FollowerCount)} and {nameof(PlaylistEntity.CreatedDate)}.")
            .Produces<SuccessResponse<IReadOnlyList<PublicPlaylistSummaryDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<IReadOnlyList<PublicPlaylistSummaryDto>>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        Guid userId,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = new GetUserPublicPlaylistsQuery(userId);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<IReadOnlyList<PublicPlaylistSummaryDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<IReadOnlyList<PublicPlaylistSummaryDto>>(result.Data!));
    }
}
