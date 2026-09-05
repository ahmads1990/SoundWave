using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;

/// <summary>
/// Exposes the HTTP endpoint for exploring and searching public playlists.
/// </summary>
internal class ListPublicPlaylistsQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/playlists/public", Handle)
            .AllowAnonymous()
            .WithTags(Constants.Tags.Playlists)
            .WithSummary("List public playlists")
            .WithDescription($"Retrieves a paginated list of public playlists with optional search term. Allowed orderBy fields: {EnumHelper.FormatAllowedValues(nameof(PlaylistEntity.FollowerCount), nameof(PlaylistEntity.CreatedDate), nameof(PlaylistEntity.Title))}.")
            .Produces<SuccessResponse<PaginatedResponse<PublicPlaylistSummaryDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PaginatedResponse<PublicPlaylistSummaryDto>>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        string? searchTerm,
        int pageIndex = 0,
        int pageSize = 20,
        string? orderBy = nameof(PlaylistEntity.FollowerCount),
        SortingDirection sortDirection = SortingDirection.Descending,
        ISender sender = default!,
        CancellationToken ct = default)
    {
        var query = new ListPublicPlaylistsQuery(searchTerm, pageIndex, pageSize, orderBy, sortDirection);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PaginatedResponse<PublicPlaylistSummaryDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<PaginatedResponse<PublicPlaylistSummaryDto>>(result.Data!));
    }
}
