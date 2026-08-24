namespace SoundWave.Playlist.Features.Tracks.AddTrackToPlaylist;

/// <summary>
/// HTTP request payload to add a track to a playlist.
/// </summary>
public record AddTrackToPlaylistRequest(Guid TrackId);
