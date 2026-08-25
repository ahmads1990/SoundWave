namespace SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;

/// <summary>
/// Represents a public playlist summary for discovery and profile views.
/// </summary>
public record PublicPlaylistSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverImageUrl,
    Guid OwnerId,
    int TrackCount,
    int FollowerCount,
    DateTime CreatedDate
);
