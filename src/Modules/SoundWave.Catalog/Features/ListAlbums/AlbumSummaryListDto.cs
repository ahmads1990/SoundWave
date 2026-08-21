using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.ListAlbums;

/// <summary>
/// Album row DTO used in the paginated list response.
/// </summary>
internal record AlbumSummaryListDto(
    Guid Id,
    string Title,
    AlbumType AlbumType,
    bool IsPublished,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    int TrackCount,
    List<AlbumSummaryListArtistDto> Artists,
    List<AlbumSummaryListGenreDto> Genres);

/// <summary>Minimal artist in the list DTO.</summary>
internal record AlbumSummaryListArtistDto(Guid ArtistId, string StageName, int Order);

/// <summary>Minimal genre in the list DTO.</summary>
internal record AlbumSummaryListGenreDto(int GenreId, string Name);
