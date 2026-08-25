namespace SoundWave.Playlist.Features.Library.GetLibrary;

/// <summary>
/// Specifies the type filter for retrieving items in the user's library.
/// </summary>
public enum LibraryItemTypeFilter
{
    All = 0,
    Playlists = 1,
    Albums = 2
}

/// <summary>
/// Specifies the sort order for items in the user's library.
/// </summary>
public enum LibrarySortBy
{
    RecentlyAdded = 0,
    Alphabetical = 1
}
