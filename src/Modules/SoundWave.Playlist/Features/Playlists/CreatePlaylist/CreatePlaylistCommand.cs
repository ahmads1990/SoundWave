using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.CreatePlaylist;

/// <summary>
/// Command for creating a new playlist.
/// </summary>
internal record CreatePlaylistCommand(
    string Title,
    string? Description,
    PlaylistVisibility Visibility)
    : IRequest<Result<PlaylistError, Guid>>;
