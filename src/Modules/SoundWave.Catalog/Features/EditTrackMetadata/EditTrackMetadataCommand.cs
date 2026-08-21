using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.EditTrackMetadata;

/// <summary>
/// Command for updating track title, duration, genres, and featured artists.
/// </summary>
internal record EditTrackMetadataCommand(
    Guid TrackId,
    string Title,
    int DurationSeconds,
    List<int>? GenreIds,
    List<Guid>? FeaturedArtistIds)
    : IRequest<Result<CatalogError, Guid>>;
