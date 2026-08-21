using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.CreateTrack;

/// <summary>
/// Command for creating a new track within an album (metadata only).
/// </summary>
internal record CreateTrackCommand(
    Guid AlbumId,
    string Title,
    int DurationSeconds = 0,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null)
    : IRequest<Result<CatalogError, Guid>>;
