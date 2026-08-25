using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.GetMyPlaylistsSimple;

/// <summary>
/// Query to retrieve a lightweight list of the current user's editable playlists.
/// Supports quick name searching and optionally indicates whether each playlist already contains a specified track.
/// </summary>
/// <param name="TrackId">Optional track identifier to check containment against.</param>
/// <param name="SearchTerm">Optional search term to filter playlists by title.</param>
internal record GetMyPlaylistsSimpleQuery(
    Guid? TrackId = null,
    string? SearchTerm = null
) : IRequest<Result<PlaylistError, IReadOnlyList<SimplePlaylistDto>>>;
