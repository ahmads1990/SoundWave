using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.CreateTrack;

/// <summary>
/// Command for creating a new track (metadata only) within an album. Audio is uploaded separately via the upload-url endpoint.
/// </summary>
public record CreateTrackCommand(
    Guid AlbumId,
    string Title,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null)
    : IRequest<Result<CatalogError, CreateTrackResponse>>;
