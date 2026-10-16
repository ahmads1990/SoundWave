namespace SoundWave.Catalog.Features.Tracks.UpdateTrack;

/// <summary>
/// Request model for updating track metadata.
/// </summary>
internal record UpdateTrackRequest(
    string Title,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null);
