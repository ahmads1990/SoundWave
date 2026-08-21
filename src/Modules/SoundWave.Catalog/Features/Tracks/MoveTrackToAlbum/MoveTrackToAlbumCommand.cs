using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.MoveTrackToAlbum;

/// <summary>
/// Command to move a track from its current album to another album owned by the same artist.
/// </summary>
internal record MoveTrackToAlbumCommand(Guid TrackId, Guid TargetAlbumId) : IRequest<Result<CatalogError, Guid>>;
