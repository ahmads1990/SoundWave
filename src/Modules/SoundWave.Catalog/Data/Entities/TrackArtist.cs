namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Junction table: Track ↔ Artist (many-to-many).
/// Composite PK: (TrackId, ArtistId).
/// </summary>
internal class TrackArtist
{
    public Guid TrackId { get; set; }
    public Guid ArtistId { get; set; }

    /// <summary>Display order of this artist on the track (0 = primary artist).</summary>
    public int Order { get; set; }

    // Navigation
    public Track Track { get; set; } = default!;
    public Artist Artist { get; set; } = default!;
}
