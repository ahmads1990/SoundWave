using SoundWave.SharedKernel.Entities;

namespace SoundWave.Playlist.Data.Entities;

/// <summary>
/// Represents a track item within a playlist.
/// </summary>
public class PlaylistTrack : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent playlist.
    /// </summary>
    public Guid PlaylistId { get; set; }

    /// <summary>
    /// Parent playlist navigation property.
    /// </summary>
    public Playlist Playlist { get; set; } = default!;

    /// <summary>
    /// Value reference to the track in the Catalog module (Catalog.Tracks).
    /// </summary>
    public Guid TrackId { get; set; }

    /// <summary>
    /// 1-based ordering position within the playlist.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Timestamp when this track was added to the playlist.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID of the user who added this track.
    /// </summary>
    public Guid AddedBy { get; set; }
}
