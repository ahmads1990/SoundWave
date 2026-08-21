using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.CreateAlbum;

/// <summary>
/// HTTP request body for creating a new album.
/// </summary>
internal record CreateAlbumRequest(
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    string? Description,
    List<int>? GenreIds,
    List<Guid>? FeaturedArtistIds);
