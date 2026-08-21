namespace SoundWave.Catalog.Features.Tracks.CreateTrack;

/// <summary>
/// HTTP request body for creating a new track within an album.
/// </summary>
internal record CreateTrackRequest(
    string Title,
    int DurationSeconds = 0,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null);
