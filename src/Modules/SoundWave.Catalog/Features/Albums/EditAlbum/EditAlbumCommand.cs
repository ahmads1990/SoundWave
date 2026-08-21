using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Albums.EditAlbum;

/// <summary>
/// Command to update an existing album's metadata, genres, and collaborating artists.
/// </summary>
internal record EditAlbumCommand(
    Guid AlbumId,
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate = null,
    string? CoverImageUrl = null,
    string? Description = null,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null
) : IRequest<Result<CatalogError, Guid>>;
