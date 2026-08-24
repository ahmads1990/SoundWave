namespace SoundWave.Playlist.Data.Entities;

/// <summary>
/// Represents a public playlist followed/saved by a user to their library.
/// </summary>
public class LikedPlaylist
{
    public Guid UserId { get; set; }
    public Guid PlaylistId { get; set; }
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;

    public Playlist Playlist { get; set; } = default!;
}
