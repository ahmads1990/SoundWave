namespace SoundWave.Catalog.Features.Tracks.CreateTrack;

/// <summary>
/// HTTP request body for creating a new track within an album.
/// </summary>
public record CreateTrackRequest(
    string Title,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null);
