using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Likes.LikeAlbum;

/// <summary>
/// Command to save/like an album to the user's library.
/// </summary>
internal record LikeAlbumCommand(Guid AlbumId) : IRequest<Result<PlaylistError, bool>>;
