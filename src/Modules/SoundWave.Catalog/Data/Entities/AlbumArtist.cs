namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Junction table: Album ↔ Artist (many-to-many).
/// Composite PK: (AlbumId, ArtistId).
/// </summary>
internal class AlbumArtist
{
    public Guid AlbumId { get; set; }
    public Guid ArtistId { get; set; }

    /// <summary>Display order of this artist on the album (0 = primary artist).</summary>
    public int Order { get; set; }

    // Navigation
    public Album Album { get; set; } = default!;
    public Artist Artist { get; set; } = default!;
}
