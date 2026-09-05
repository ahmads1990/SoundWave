using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Common;

internal static class Constants
{
    internal const string SCHEMA_NAME = "Catalog";

    internal static class Tags
    {
        internal const string Genres = "Genres";
        internal const string Albums = "Albums";
        internal const string Tracks = "Tracks";
        internal const string Artists = "Artists";
    }

    internal static class Caching
    {
        private const string GenresListPrefix = "catalog:genres:";
        internal const int GenresListTtlMinutes = 60 * 24; // 24 hours

        private const string ArtistProfilePrefix = "catalog:artist:";
        internal const int ArtistProfileTtlMinutes = 60; // 1 hour

        private const string NewReleasesPrefix = "catalog:new-releases:";
        internal const int NewReleasesTtlMinutes = 15; // 15 minutes

        private const string AlbumsListPrefix = "catalog:albums:";
        internal const int AlbumsListTtlMinutes = 10; // 10 minutes

        internal static string GetListGenresKey(int pageIndex, int pageSize, string? name, GenreType? type, string? orderBy, SortingDirection sortDirection)
            => $"{GenresListPrefix}idx:{pageIndex}:sz:{pageSize}:name:{name}:type:{type}:order:{orderBy}:dir:{sortDirection}";

        internal static string GetArtistProfileKey(Guid artistId) => $"{ArtistProfilePrefix}{artistId}";

        internal static string GetNewReleasesKey(int pageIndex, int pageSize, int? genreId, AlbumType? albumType, int? daysOld)
            => $"{NewReleasesPrefix}idx:{pageIndex}:sz:{pageSize}:genre:{genreId}:type:{albumType}:days:{daysOld}";

        internal static string GetListAlbumsKey(int pageIndex, int pageSize, string? title, int? genreId, Guid? artistId, bool? isPublished, AlbumType? albumType, string? orderBy, SortingDirection sortDirection)
            => $"{AlbumsListPrefix}idx:{pageIndex}:sz:{pageSize}:title:{title}:genre:{genreId}:artist:{artistId}:pub:{isPublished}:type:{albumType}:order:{orderBy}:dir:{sortDirection}";
    }
}
