using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.DeleteTrack;

/// <summary>
/// Command to soft-delete a track from an album and re-gap remaining track numbers.
/// </summary>
internal record DeleteTrackCommand(Guid TrackId) : IRequest<Result<CatalogError, Guid>>;
