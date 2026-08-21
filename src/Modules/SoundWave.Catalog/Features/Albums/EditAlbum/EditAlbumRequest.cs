using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.Albums.EditAlbum;

/// <summary>
/// Request DTO for updating an album's metadata.
/// </summary>
internal record EditAlbumRequest(
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate = null,
    string? CoverImageUrl = null,
    string? Description = null,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null
);
