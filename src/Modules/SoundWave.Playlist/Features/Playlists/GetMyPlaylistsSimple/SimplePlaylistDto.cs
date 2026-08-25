namespace SoundWave.Playlist.Features.Playlists.GetMyPlaylistsSimple;

/// <summary>
/// Lightweight playlist summary for quick menus and 'Add to Playlist' modal.
/// </summary>
public record SimplePlaylistDto(
    Guid Id,
    string Title,
    string? CoverImageUrl,
    int TrackCount,
    bool ContainsTrack
);
