using SoundWave.Playlist.Common.Enums;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Playlist.Data.Entities;

/// <summary>
/// Represents a user-created or system-managed playlist of tracks.
/// </summary>
public class Playlist : BaseEntity
{
    /// <summary>
    /// The user ID of the creator/owner (value reference to Identity.Users).
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The name of the playlist.
    /// </summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// Optional description of the playlist.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL to the playlist cover image.
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Visibility setting: Private (0), Public (1), Collaborative (2).
    /// </summary>
    public PlaylistVisibility Visibility { get; set; } = PlaylistVisibility.Public;

    /// <summary>
    /// True for system-generated playlists such as "Liked Songs" that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Denormalized count of active tracks in this playlist.
    /// </summary>
    public int TrackCount { get; set; } = 0;

    /// <summary>
    /// Denormalized total duration in seconds of all tracks in this playlist.
    /// </summary>
    public int TotalDurationSeconds { get; set; } = 0;

    /// <summary>
    /// Denormalized count of users who have liked/saved this playlist.
    /// </summary>
    public int FollowerCount { get; set; } = 0;

    /// <summary>
    /// Tracks contained within this playlist.
    /// </summary>
    public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();

    /// <summary>
    /// Collaborators authorized to edit this playlist (if collaborative).
    /// </summary>
    public ICollection<PlaylistCollaborator> Collaborators { get; set; } = new List<PlaylistCollaborator>();
}
