namespace SoundWave.Playlist.Features.Library.GetLibrary;

/// <summary>
/// Represents an aggregated item in the user's library (owned playlist, saved playlist, or saved album).
/// </summary>
public record LibraryItemDto(
    Guid Id,
    string Title,
    string ItemType, // "Playlist", "SystemPlaylist", "Album"
    string? CoverImageUrl,
    int TrackCount,
    Guid OwnerId,
    DateTime AddedAt,
    string? Subtitle
);
