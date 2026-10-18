namespace SoundWave.Catalog.Features.Tracks.BatchCreateTracks;

/// <summary>
/// HTTP request model for batch creating tracks in an album.
/// </summary>
public record BatchCreateTracksRequest(
    IReadOnlyList<BatchTrackItemRequest> Tracks);

/// <summary>
/// Request parameters for a single track within the batch.
/// </summary>
public record BatchTrackItemRequest(
    string Title,
    string FileName,
    long FileSizeBytes,
    string? ContentType = null,
    IReadOnlyList<int>? GenreIds = null,
    IReadOnlyList<Guid>? FeaturedArtistIds = null);
