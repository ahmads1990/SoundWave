namespace SoundWave.Playlist.Common;

/// <summary>
/// Domain-specific errors for the Playlist module.
/// </summary>
internal enum PlaylistError
{
    None = 0,
    PlaylistNotFound = 1,
    Unauthorized = 2,
    SystemPlaylistProtected = 3,
    InvalidTrack = 4,
    TrackAlreadyInPlaylist = 5,
    TrackNotInPlaylist = 6,
    UserNotAuthenticated = 7,
    ValidationFailed = 8
}
