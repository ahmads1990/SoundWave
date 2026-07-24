using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Common;

internal static class Constants
{
    internal const string SCHEMA_NAME = "Catalog";
    internal const string MODULE_TAG = "Catalog";

    internal static class Caching
    {
        private const string GenresListPrefix = "catalog:genres:";
        internal const int GenresListTtlMinutes = 60 * 24; // 24 hours

        private const string ArtistProfilePrefix = "catalog:artist:";
        internal const int ArtistProfileTtlMinutes = 60; // 1 hour

        internal static string GetListGenresKey(int pageIndex, int pageSize, string? name, GenreType? type, string? orderBy, SortingDirection sortDirection)
            => $"{GenresListPrefix}idx:{pageIndex}:sz:{pageSize}:name:{name}:type:{type}:order:{orderBy}:dir:{sortDirection}";

        internal static string GetArtistProfileKey(Guid artistId) => $"{ArtistProfilePrefix}{artistId}";
    }

    internal static class MessageBus
    {
        /// <summary>Topic exchange for all Catalog module events.</summary>
        internal const string Exchange = "soundwave.catalog";

        internal static class RoutingKeys
        {
            internal const string ArtistApproved = "artist.approved";
            internal const string TrackUploaded  = "track.uploaded";
            internal const string TrackReady     = "track.ready";
        }
    }
}
