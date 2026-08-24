using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.EditPlaylist;

/// <summary>
/// Command for updating metadata of an existing playlist.
/// </summary>
internal record EditPlaylistCommand(
    Guid Id,
    string Title,
    string? Description,
    PlaylistVisibility Visibility)
    : IRequest<Result<PlaylistError, bool>>;
