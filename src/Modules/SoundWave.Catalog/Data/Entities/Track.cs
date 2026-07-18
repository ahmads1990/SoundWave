using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Represents a single track within an album.
/// </summary>
internal class Track : BaseEntity
{
    public Guid AlbumId { get; set; }
    public string Title { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }
    public int TrackNumber { get; set; }

    /// <summary>
    /// Denormalized play count. Authoritative value lives in Redis and is periodically
    /// flushed here by the play count flush BackgroundService.
    /// </summary>
    public long PlayCount { get; set; }

    /// <summary>Denormalized like count — incremented/decremented by LikeTrackCommand.</summary>
    public int LikeCount { get; set; }

    // Navigation
    public Album Album { get; set; } = default!;
    public TrackFile? TrackFile { get; set; }
    public ICollection<TrackArtist> TrackArtists { get; set; } = [];
    public ICollection<TrackGenre> TrackGenres { get; set; } = [];
}
