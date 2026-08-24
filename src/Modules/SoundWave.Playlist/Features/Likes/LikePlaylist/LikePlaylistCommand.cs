using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Likes.LikePlaylist;

/// <summary>
/// Command to save/follow a public playlist to the user's library and increment its follower count.
/// </summary>
internal record LikePlaylistCommand(Guid PlaylistId) : IRequest<Result<PlaylistError, bool>>;
