using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Features.Playlists.GetPlaylist;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.GetLikedSongsPlaylist;

/// <summary>
/// Query to retrieve the current user's system "Liked Songs" playlist.
/// </summary>
internal record GetLikedSongsPlaylistQuery : IRequest<Result<PlaylistError, PlaylistDetailDto>>;
