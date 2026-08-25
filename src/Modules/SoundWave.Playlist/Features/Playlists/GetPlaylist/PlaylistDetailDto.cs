using SoundWave.Playlist.Common.Enums;

namespace SoundWave.Playlist.Features.Playlists.GetPlaylist;

/// <summary>
/// Represents full playlist details for the /playlist/:id view.
/// </summary>
public record PlaylistDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverImageUrl,
    Guid OwnerId,
    PlaylistVisibility Visibility,
    bool IsSystem,
    int TrackCount,
    int TotalDurationSeconds,
    int FollowerCount,
    bool IsLikedByCurrentUser,
    bool IsOwner,
    IReadOnlyList<PlaylistTrackItemDto> Tracks
);

/// <summary>
/// Represents a track item within a playlist.
/// </summary>
public record PlaylistTrackItemDto(
    Guid Id,
    Guid TrackId,
    int Position,
    DateTime CreatedDate,
    Guid? CreatedBy
);
