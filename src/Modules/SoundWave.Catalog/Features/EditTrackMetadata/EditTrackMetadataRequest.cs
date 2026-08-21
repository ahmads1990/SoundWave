namespace SoundWave.Catalog.Features.EditTrackMetadata;

/// <summary>
/// HTTP request body for editing track metadata.
/// </summary>
internal record EditTrackMetadataRequest(
    string Title,
    int DurationSeconds,
    List<int>? GenreIds,
    List<Guid>? FeaturedArtistIds);
