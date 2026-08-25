using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.GetPlaylist;

/// <summary>
/// Query to retrieve full details of a playlist including its ordered tracks.
/// </summary>
/// <param name="PlaylistId">The unique identifier of the playlist.</param>
internal record GetPlaylistQuery(Guid PlaylistId) : IRequest<Result<PlaylistError, PlaylistDetailDto>>;
