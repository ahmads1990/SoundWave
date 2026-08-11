namespace SoundWave.Catalog.Features.GetArtistProfile;

internal record ArtistTopTrackDto(
    Guid Id,
    string Title,
    int DurationSeconds,
    int TrackNumber,
    long PlayCount,
    int LikeCount,
    Guid AlbumId,
    string AlbumTitle,
    string? AlbumCoverImageUrl);
