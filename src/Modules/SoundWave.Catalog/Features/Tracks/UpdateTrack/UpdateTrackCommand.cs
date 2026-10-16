using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.UpdateTrack;

/// <summary>
/// Command for updating track title, duration, genres, and featured artists.
/// </summary>
internal record UpdateTrackCommand(
    Guid TrackId,
    string Title,
    List<int>? GenreIds,
    List<Guid>? FeaturedArtistIds)
    : IRequest<Result<CatalogError, Guid>>;
