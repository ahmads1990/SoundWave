using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;

/// <summary>
/// Query to search and explore public playlists with pagination, filtering, and Redis caching.
/// </summary>
/// <param name="SearchTerm">Optional search term matching playlist title or description.</param>
/// <param name="PageIndex">Zero-based page index. Defaults to 0.</param>
/// <param name="PageSize">Page size. Defaults to 20.</param>
/// <param name="OrderBy">Sort column (e.g. FollowerCount, CreatedDate, Title). Defaults to FollowerCount.</param>
/// <param name="SortDirection">Sort direction (Ascending / Descending). Defaults to Descending.</param>
internal record ListPublicPlaylistsQuery(
    string? SearchTerm = null,
    int PageIndex = 0,
    int PageSize = 20,
    string? OrderBy = nameof(PlaylistEntity.FollowerCount),
    SortingDirection SortDirection = SortingDirection.Descending
) : IRequest<Result<PlaylistError, PaginatedResponse<PublicPlaylistSummaryDto>>>;
