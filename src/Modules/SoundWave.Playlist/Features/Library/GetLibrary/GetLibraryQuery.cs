using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Library.GetLibrary;

/// <summary>
/// Query to retrieve an aggregated view of the current user's library items.
/// </summary>
/// <param name="Type">Filter by item type. Defaults to All.</param>
/// <param name="SortBy">Sort order. Defaults to RecentlyAdded.</param>
internal record GetLibraryQuery(
    LibraryItemTypeFilter Type = LibraryItemTypeFilter.All,
    LibrarySortBy SortBy = LibrarySortBy.RecentlyAdded
) : IRequest<Result<PlaylistError, IReadOnlyList<LibraryItemDto>>>;
