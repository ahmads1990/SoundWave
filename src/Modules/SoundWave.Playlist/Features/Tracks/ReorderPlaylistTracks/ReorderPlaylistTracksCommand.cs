using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Tracks.ReorderPlaylistTracks;

/// <summary>
/// Command to change the position of a track within a playlist and shift intermediate tracks.
/// </summary>
internal record ReorderPlaylistTracksCommand(
    Guid PlaylistId,
    Guid TrackId,
    int NewPosition) : IRequest<Result<PlaylistError, bool>>;
