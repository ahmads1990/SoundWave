namespace SoundWave.Catalog.Features.Tracks.BatchCreateTracks;

/// <summary>
/// Response returned after successfully batch creating tracks and generating presigned audio upload URLs.
/// </summary>
public record BatchCreateTracksResponse(
    Guid AlbumId,
    IReadOnlyList<BatchTrackUploadItemResponse> Tracks);

/// <summary>
/// Individual track upload details containing track identifier and presigned S3 PUT URL.
/// </summary>
public record BatchTrackUploadItemResponse(
    Guid TrackId,
    int TrackNumber,
    string Title,
    string UploadUrl,
    string StorageKey,
    DateTime ExpiresAt);
