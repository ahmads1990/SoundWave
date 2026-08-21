using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.Artists.GetArtistProfile;

internal record ArtistAlbumDto(
    Guid Id,
    string Title,
    AlbumType AlbumType,
    DateTime? ReleaseDate,
    string? CoverImageUrl,
    int TrackCount);
