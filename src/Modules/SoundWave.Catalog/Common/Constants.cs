namespace SoundWave.Catalog.Common;

internal static class Constants
{
    internal const string SCHEMA_NAME = "Catalog";
    internal const string MODULE_TAG = "Catalog";

    internal static class Caching
    {
        private const string GenresListPrefix = "catalog:genres:all";
        internal const int GenresListTtlMinutes = 60 * 24; // 24 hours

        private const string ArtistProfilePrefix = "catalog:artist:";
        internal const int ArtistProfileTtlMinutes = 60; // 1 hour

        internal static string GetGenresListKey() => GenresListPrefix;
        internal static string GetArtistProfileKey(Guid artistId) => $"{ArtistProfilePrefix}{artistId}";
    }

    internal static class Pagination
    {
        internal const int DefaultPage = 1;
        internal const int DefaultPageSize = 20;
        internal const int MaxPageSize = 100;
    }
}
