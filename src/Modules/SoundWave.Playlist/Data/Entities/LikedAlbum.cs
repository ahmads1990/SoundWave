namespace SoundWave.Playlist.Data.Entities;

/// <summary>
/// Represents an album saved to a user's library.
/// </summary>
public class LikedAlbum
{
    public Guid UserId { get; set; }
    public Guid AlbumId { get; set; }
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}
