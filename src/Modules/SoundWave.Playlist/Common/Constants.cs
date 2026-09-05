using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Common;

internal static class Constants
{
    public const string SCHEMA_NAME = "Playlist";
    public const string LikedSongsPlaylistTitle = "Liked Songs";

    internal static class Tags
    {
        public const string Playlists = "Playlists";
        public const string PlaylistTracks = "Playlist Tracks";
        public const string Likes = "Likes";
        public const string Library = "Library";
    }

    internal static class Caching
    {
        private const string PublicPlaylistsListPrefix = "playlists:public:";
        internal const int PublicPlaylistsListTtlMinutes = 5;

        internal static string GetPublicPlaylistsKey(int pageIndex, int pageSize, string? searchTerm, string? orderBy, SortingDirection sortDirection)
            => $"{PublicPlaylistsListPrefix}idx:{pageIndex}:sz:{pageSize}:q:{searchTerm}:order:{orderBy}:dir:{sortDirection}";
    }
}

