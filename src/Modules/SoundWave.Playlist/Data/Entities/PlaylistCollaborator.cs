namespace SoundWave.Playlist.Data.Entities;

/// <summary>
/// Represents a collaborator authorized to add/remove tracks in a collaborative playlist.
/// </summary>
public class PlaylistCollaborator
{
    public Guid PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = default!;

    public Guid UserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string? Role { get; set; }
}
