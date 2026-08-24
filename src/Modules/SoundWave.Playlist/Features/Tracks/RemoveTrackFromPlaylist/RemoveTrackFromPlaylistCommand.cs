using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Tracks.RemoveTrackFromPlaylist;

/// <summary>
/// Command to remove a track from a playlist and re-gap remaining track positions.
/// </summary>
internal record RemoveTrackFromPlaylistCommand(
    Guid PlaylistId,
    Guid TrackId) : IRequest<Result<PlaylistError, bool>>;
