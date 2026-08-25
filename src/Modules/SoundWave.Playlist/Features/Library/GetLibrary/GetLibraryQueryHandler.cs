using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Library.GetLibrary;

/// <summary>
/// Handles aggregating and filtering all items saved in the user's library (playlists and albums).
/// Acts as a unified read model for the Spotify/Apple Music style sidebar and library feed.
/// </summary>
internal class GetLibraryQueryHandler(
    PlaylistReadDbContext readDbContext,
    ICurrentUserService currentUserService,
    ILogger<GetLibraryQueryHandler> logger)
    : IRequestHandler<GetLibraryQuery, Result<PlaylistError, IReadOnlyList<LibraryItemDto>>>
{
    /// <summary>
    /// Executes the library aggregation query for the currently authenticated user.
    /// Dispatches retrieval of owned playlists, followed playlists, and liked albums based on the requested filter.
    /// </summary>
    /// <param name="request">The library query containing item type and sort criteria.</param>
    /// <param name="cancellationToken">Cancellation token for database operations.</param>
    /// <returns>A Result containing the sorted list of library item DTOs.</returns>
    public async Task<Result<PlaylistError, IReadOnlyList<LibraryItemDto>>> Handle(
        GetLibraryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var items = await AggregateLibraryItemsAsync(userId, request.Type, cancellationToken);
        var sorted = ApplySorting(items, request.SortBy);

        logger.LogInformation("Retrieved {Count} library items for user {UserId} (Type: {Type})", sorted.Count, userId, request.Type);
        return Result<PlaylistError, IReadOnlyList<LibraryItemDto>>.Success(sorted);
    }

    #region Private Methods

    /// <summary>
    /// Aggregates library items from multiple database tables according to the specified <paramref name="type"/> filter.
    /// Evaluates if playlist queries, album queries, or both should be executed.
    /// </summary>
    /// <param name="userId">The unique identifier of the authenticated user.</param>
    /// <param name="type">The type filter selecting All items, Playlists only, or Albums only.</param>
    /// <param name="cancellationToken">Cancellation token for database operations.</param>
    /// <returns>A combined list of un-sorted <see cref="LibraryItemDto"/> items.</returns>
    private async Task<List<LibraryItemDto>> AggregateLibraryItemsAsync(
        Guid userId,
        LibraryItemTypeFilter type,
        CancellationToken cancellationToken)
    {
        var items = new List<LibraryItemDto>();

        if (type is LibraryItemTypeFilter.All or LibraryItemTypeFilter.Playlists)
        {
            var playlists = await GetPlaylistsAsync(userId, cancellationToken);
            items.AddRange(playlists);
        }

        if (type is LibraryItemTypeFilter.All or LibraryItemTypeFilter.Albums)
        {
            var albums = await GetLikedAlbumsAsync(userId, cancellationToken);
            items.AddRange(albums);
        }

        return items;
    }

    /// <summary>
    /// Queries the read database for both owned playlists and followed public playlists.
    /// Projects them into a unified <see cref="LibraryItemDto"/> format with formatted subtitles.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose library is being read.</param>
    /// <param name="cancellationToken">Cancellation token for database operations.</param>
    /// <returns>A list containing both owned and followed playlist DTOs.</returns>
    private async Task<List<LibraryItemDto>> GetPlaylistsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // 1. Fetch user-owned playlists (excluding soft-deleted)
        var owned = await readDbContext.Playlists
            .Where(p => p.OwnerId == userId && !p.IsDeleted)
            .Select(p => new LibraryItemDto(
                p.Id,
                p.Title,
                p.IsSystem ? "SystemPlaylist" : "Playlist",
                p.CoverImageUrl,
                p.TrackCount,
                p.OwnerId,
                p.CreatedDate,
                p.IsSystem ? $"{p.TrackCount} songs" : $"Playlist • {p.TrackCount} songs"
            ))
            .ToListAsync(cancellationToken);

        // 2. Fetch public playlists followed by the user
        var liked = await readDbContext.LikedPlaylists
            .Where(lp => lp.UserId == userId && lp.Playlist.Visibility == PlaylistVisibility.Public && !lp.Playlist.IsDeleted)
            .Select(lp => new LibraryItemDto(
                lp.PlaylistId,
                lp.Playlist.Title,
                "Playlist",
                lp.Playlist.CoverImageUrl,
                lp.Playlist.TrackCount,
                lp.Playlist.OwnerId,
                lp.LikedAt,
                $"Playlist • {lp.Playlist.TrackCount} songs"
            ))
            .ToListAsync(cancellationToken);

        owned.AddRange(liked);
        return owned;
    }

    /// <summary>
    /// Queries the read database for saved/liked albums associated with the user.
    /// Projects album bookmarks into the unified <see cref="LibraryItemDto"/> structure.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token for database operations.</param>
    /// <returns>A list of saved album DTOs.</returns>
    private async Task<List<LibraryItemDto>> GetLikedAlbumsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await readDbContext.LikedAlbums
            .Where(la => la.UserId == userId)
            .Select(la => new LibraryItemDto(
                la.AlbumId,
                "Saved Album",
                "Album",
                null,
                0,
                Guid.Empty,
                la.LikedAt,
                "Album • Saved"
            ))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Applies in-memory ordering to the aggregated library items list.
    /// Supports Alphabetical sorting (by title ascending) and RecentlyAdded sorting (by added timestamp descending).
    /// </summary>
    /// <param name="items">The unsorted collection of library items.</param>
    /// <param name="sortBy">The sort criteria specified by the client.</param>
    /// <returns>An ordered read-only list of library items.</returns>
    private static IReadOnlyList<LibraryItemDto> ApplySorting(
        List<LibraryItemDto> items,
        LibrarySortBy sortBy)
    {
        return sortBy switch
        {
            LibrarySortBy.Alphabetical => items.OrderBy(i => i.Title).ToList(),
            _                          => items.OrderByDescending(i => i.AddedAt).ToList()
        };
    }

    #endregion
}
