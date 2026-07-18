namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Junction table: Album ↔ Genre (many-to-many).
/// Composite PK: (AlbumId, GenreId).
/// </summary>
internal class AlbumGenre
{
    public Guid AlbumId { get; set; }
    public int GenreId { get; set; }

    // Navigation
    public Album Album { get; set; } = default!;
    public Genre Genre { get; set; } = default!;
}
