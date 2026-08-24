using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Likes.UnlikeAlbum;

/// <summary>
/// Command to remove/unsave an album from the user's library.
/// </summary>
internal record UnlikeAlbumCommand(Guid AlbumId) : IRequest<Result<PlaylistError, bool>>;
