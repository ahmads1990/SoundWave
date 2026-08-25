using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Common;

internal static class Constants
{
    public const string SCHEMA_NAME = "Playlist";
    public const string MODULE_TAG = "Playlists";
    public const string LikedSongsPlaylistTitle = "Liked Songs";

    internal static class Caching
    {
        private const string PublicPlaylistsListPrefix = "playlists:public:";
        internal const int PublicPlaylistsListTtlMinutes = 5;

        internal static string GetPublicPlaylistsKey(int pageIndex, int pageSize, string? searchTerm, string? orderBy, SortingDirection sortDirection)
            => $"{PublicPlaylistsListPrefix}idx:{pageIndex}:sz:{pageSize}:q:{searchTerm}:order:{orderBy}:dir:{sortDirection}";
    }
}

