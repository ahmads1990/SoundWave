namespace SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;

/// <summary>
/// Response returned when a presigned track upload URL is generated.
/// </summary>
/// <param name="TrackId">The track identifier.</param>
/// <param name="UploadUrl">The presigned S3 PUT URL to which the client uploads the raw audio stream.</param>
/// <param name="StorageKey">The destination S3 storage key.</param>
/// <param name="ExpiresAt">The UTC timestamp at which the presigned URL expires.</param>
internal record TrackUploadUrlResponse(
    Guid TrackId,
    string UploadUrl,
    string StorageKey,
    DateTime ExpiresAt);
