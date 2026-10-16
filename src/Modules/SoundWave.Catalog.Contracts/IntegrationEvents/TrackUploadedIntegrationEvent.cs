namespace SoundWave.Catalog.Contracts.IntegrationEvents;

/// <summary>
/// Integration event published to the message bus via the transactional outbox
/// when raw audio has been successfully uploaded for a track and is pending FFmpeg transcoding.
/// </summary>
/// <param name="TrackId">The unique track identifier.</param>
/// <param name="AlbumId">The parent album identifier.</param>
/// <param name="ArtistId">The primary artist identifier who owns the track.</param>
/// <param name="RawStorageKey">The S3 object key where the raw audio file resides (e.g. audio/raw/{trackId}.mp3).</param>
/// <param name="OriginalFileName">The original file name uploaded by the artist.</param>
/// <param name="FileSizeBytes">The raw audio file size in bytes.</param>
public record TrackUploadedIntegrationEvent(
    Guid TrackId,
    Guid AlbumId,
    Guid ArtistId,
    string RawStorageKey,
    string OriginalFileName,
    long FileSizeBytes);
