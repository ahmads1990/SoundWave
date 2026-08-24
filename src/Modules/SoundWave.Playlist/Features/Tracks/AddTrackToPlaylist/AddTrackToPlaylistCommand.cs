using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Tracks.AddTrackToPlaylist;

/// <summary>
/// Command to append a track to a playlist at the end of the track list.
/// </summary>
internal record AddTrackToPlaylistCommand(
    Guid PlaylistId,
    Guid TrackId) : IRequest<Result<PlaylistError, Guid>>;
