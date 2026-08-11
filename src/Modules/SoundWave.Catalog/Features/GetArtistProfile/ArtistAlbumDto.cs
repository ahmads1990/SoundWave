using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.GetArtistProfile;

internal record ArtistAlbumDto(
    Guid Id,
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    int TrackCount);
