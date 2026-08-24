using SoundWave.Playlist.Common.Enums;

namespace SoundWave.Playlist.Features.Playlists.EditPlaylist;

/// <summary>
/// HTTP request body for editing an existing playlist.
/// </summary>
public record EditPlaylistRequest(
    string Title,
    string? Description = null,
    PlaylistVisibility Visibility = PlaylistVisibility.Public);
