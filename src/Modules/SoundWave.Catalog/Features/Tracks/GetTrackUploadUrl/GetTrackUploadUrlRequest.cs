namespace SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;

/// <summary>
/// Request payload to request a presigned S3 upload URL for track audio.
/// </summary>
/// <param name="FileName">Original audio file name including extension (e.g. "song.mp3", "master.flac").</param>
/// <param name="FileSizeBytes">File size in bytes (max 50 MB).</param>
/// <param name="ContentType">Optional MIME content type (e.g. "audio/mpeg", "audio/flac").</param>
internal record GetTrackUploadUrlRequest(
    string FileName,
    long FileSizeBytes,
    string? ContentType = null);
