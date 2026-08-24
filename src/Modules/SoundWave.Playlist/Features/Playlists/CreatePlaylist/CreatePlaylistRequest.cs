using SoundWave.Playlist.Common.Enums;

namespace SoundWave.Playlist.Features.Playlists.CreatePlaylist;

/// <summary>
/// HTTP request body for creating a new playlist.
/// </summary>
public record CreatePlaylistRequest(
    string Title,
    string? Description = null,
    PlaylistVisibility Visibility = PlaylistVisibility.Public);
