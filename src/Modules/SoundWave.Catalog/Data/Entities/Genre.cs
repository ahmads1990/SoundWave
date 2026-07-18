namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Lookup table for music genres and moods.
/// Uses an int PK (not Guid) — static reference data seeded at migration time, never created by user actions.
/// </summary>
internal class Genre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Common.GenreType Type { get; set; }

    // Navigation
    public ICollection<TrackGenre> TrackGenres { get; set; } = [];
    public ICollection<AlbumGenre> AlbumGenres { get; set; } = [];
}
