using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.GetTrackStatus;

/// <summary>
/// Status and processing details for a track file.
/// </summary>
/// <param name="TrackId">The track identifier.</param>
/// <param name="Status">The audio file status (PendingUpload, PendingValidation, Processing, Available, Failed); null when no audio has been uploaded yet.</param>
/// <param name="FailureReason">Error output if processing failed.</param>
/// <param name="ProcessingStartedAt">UTC timestamp when FFmpeg processing started.</param>
/// <param name="HlsPlaylistPath">S3 storage key or URL for the master HLS playlist (.m3u8).</param>
/// <param name="PreviewPlaylistPath">S3 storage key or URL for the preview HLS playlist (.m3u8).</param>
internal record TrackStatusResponse(
    Guid TrackId,
    FileStatus? Status,
    string? FailureReason,
    DateTime? ProcessingStartedAt,
    string? HlsPlaylistPath,
    string? PreviewPlaylistPath);
