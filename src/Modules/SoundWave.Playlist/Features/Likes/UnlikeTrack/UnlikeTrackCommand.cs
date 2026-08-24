using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Likes.UnlikeTrack;

/// <summary>
/// Command to unlike a track, removing it from the user's liked tracks and system "Liked Songs" playlist.
/// </summary>
internal record UnlikeTrackCommand(Guid TrackId) : IRequest<Result<PlaylistError, bool>>;
