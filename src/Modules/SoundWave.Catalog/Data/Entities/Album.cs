using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Represents a music album (Album, EP, or Single).
/// </summary>
internal class Album : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public AlbumType AlbumType { get; set; }

    /// <summary>Only published albums are visible to listeners.</summary>
    public bool IsPublished { get; set; }

    public DateTime? ReleaseDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// Denormalized track count. Updated when tracks are added to or removed from the album.
    /// </summary>
    public int TrackCount { get; set; }

    // Navigation
    public ICollection<Track> Tracks { get; set; } = [];
    public ICollection<AlbumArtist> AlbumArtists { get; set; } = [];
    public ICollection<AlbumGenre> AlbumGenres { get; set; } = [];
}
