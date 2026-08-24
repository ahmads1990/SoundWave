using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.DeletePlaylist;

/// <summary>
/// Command for soft-deleting an existing playlist.
/// </summary>
internal record DeletePlaylistCommand(Guid Id) : IRequest<Result<PlaylistError, bool>>;
