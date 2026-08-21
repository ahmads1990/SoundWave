using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.GetNewReleases;

/// <summary>
/// Summary DTO for an album in the new releases list.
/// </summary>
internal record AlbumSummaryDto(
    Guid Id,
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    int TrackCount,
    List<AlbumSummaryArtistDto> Artists);

/// <summary>
/// Minimal artist attribution in a summary DTO.
/// </summary>
internal record AlbumSummaryArtistDto(Guid ArtistId, string StageName, int Order);
