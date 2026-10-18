using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.BatchCreateTracks;

/// <summary>
/// Individual track payload within a batch creation command.
/// </summary>
public record BatchTrackItem(
    string Title,
    string FileName,
    long FileSizeBytes,
    string? ContentType = null,
    IReadOnlyList<int>? GenreIds = null,
    IReadOnlyList<Guid>? FeaturedArtistIds = null);

/// <summary>
/// Command to create multiple tracks for an unpublished album in a single database transaction
/// and generate presigned S3 PUT upload URLs for each track.
/// </summary>
public record BatchCreateTracksCommand(
    Guid AlbumId,
    IReadOnlyList<BatchTrackItem> Tracks)
    : IRequest<Result<CatalogError, BatchCreateTracksResponse>>;
