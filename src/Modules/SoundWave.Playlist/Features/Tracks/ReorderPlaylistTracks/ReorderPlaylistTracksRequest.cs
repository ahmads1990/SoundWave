namespace SoundWave.Playlist.Features.Tracks.ReorderPlaylistTracks;

/// <summary>
/// HTTP request payload to reorder a track in a playlist.
/// </summary>
public record ReorderPlaylistTracksRequest(
    Guid TrackId,
    int NewPosition);
