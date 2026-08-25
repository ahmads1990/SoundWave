using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.GetUserPublicPlaylists;

/// <summary>
/// Query to retrieve all public playlists created by a specific user or artist profile.
/// </summary>
/// <param name="UserId">The target user or artist identifier.</param>
internal record GetUserPublicPlaylistsQuery(Guid UserId)
    : IRequest<Result<PlaylistError, IReadOnlyList<PublicPlaylistSummaryDto>>>;
