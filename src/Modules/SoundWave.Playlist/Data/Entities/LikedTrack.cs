namespace SoundWave.Playlist.Data.Entities;

/// <summary>
/// Represents a user's liked/favorited track.
/// </summary>
public class LikedTrack
{
    public Guid UserId { get; set; }
    public Guid TrackId { get; set; }
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}
