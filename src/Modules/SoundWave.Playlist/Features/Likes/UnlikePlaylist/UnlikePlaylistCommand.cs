using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Likes.UnlikePlaylist;

/// <summary>
/// Command to remove/unfollow a playlist from the user's library and decrement its follower count.
/// </summary>
internal record UnlikePlaylistCommand(Guid PlaylistId) : IRequest<Result<PlaylistError, bool>>;
