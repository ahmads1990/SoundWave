using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.GetTrackStatus;

/// <summary>
/// Query to retrieve the current processing status and metadata of a track file.
/// </summary>
/// <param name="TrackId">The track identifier.</param>
internal record GetTrackStatusQuery(Guid TrackId)
    : IRequest<Result<CatalogError, TrackStatusResponse>>;
