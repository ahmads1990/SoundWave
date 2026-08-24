using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Likes.LikeTrack;

/// <summary>
/// Command to like a track, adding it to the user's liked tracks and system "Liked Songs" playlist.
/// </summary>
internal record LikeTrackCommand(Guid TrackId) : IRequest<Result<PlaylistError, bool>>;
