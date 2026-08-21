using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Albums.CreateAlbum;

/// <summary>
/// Command for creating a new album in the catalog module.
/// </summary>
internal record CreateAlbumCommand(
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    string? Description,
    List<int>? GenreIds,
    List<Guid>? FeaturedArtistIds)
    : IRequest<Result<CatalogError, Guid>>;
