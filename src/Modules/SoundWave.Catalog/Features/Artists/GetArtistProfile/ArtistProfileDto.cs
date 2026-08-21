namespace SoundWave.Catalog.Features.Artists.GetArtistProfile;

internal record ArtistProfileDto(
    Guid Id,
    Guid UserId,
    string StageName,
    string? Bio,
    int FollowerCount,
    int MonthlyListeners,
    long TotalStreams,
    DateTime? ApprovedAt,
    IReadOnlyList<ArtistTopTrackDto> TopTracks,
    IReadOnlyList<ArtistAlbumDto> Albums);
