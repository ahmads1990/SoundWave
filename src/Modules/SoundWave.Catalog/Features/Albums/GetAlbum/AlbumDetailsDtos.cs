using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.Albums.GetAlbum;

/// <summary>Track row in the album detail view.</summary>
internal record AlbumTrackDto(
    Guid Id,
    string Title,
    int DurationSeconds,
    int TrackNumber,
    long PlayCount,
    int LikeCount,
    TrackFileStatus FileStatus,
    List<AlbumTrackArtistDto> Artists);

/// <summary>Artist attribution on a track.</summary>
internal record AlbumTrackArtistDto(Guid ArtistId, string StageName, int Order);

/// <summary>Genre tag on the album.</summary>
internal record AlbumGenreDto(int Id, string Name);

/// <summary>Full album detail DTO including ordered tracklist.</summary>
internal record AlbumDetailsDto(
    Guid Id,
    string Title,
    AlbumType AlbumType,
    bool IsPublished,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    string? Description,
    int TrackCount,
    List<AlbumTrackDto> Tracks,
    List<AlbumGenreDto> Genres,
    List<AlbumArtistDto> Artists);

/// <summary>Artist attribution on the album.</summary>
internal record AlbumArtistDto(Guid ArtistId, string StageName, int Order);
