namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Junction table: Track ↔ Genre (many-to-many).
/// Composite PK: (TrackId, GenreId).
/// </summary>
internal class TrackGenre
{
    public Guid TrackId { get; set; }
    public int GenreId { get; set; }

    // Navigation
    public Track Track { get; set; } = default!;
    public Genre Genre { get; set; } = default!;
}
