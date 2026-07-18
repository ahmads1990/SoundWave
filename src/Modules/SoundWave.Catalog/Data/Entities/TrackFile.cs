using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Processing and HLS metadata for a track. 1:1 relationship with <see cref="Track"/>.
/// Created when a track file is uploaded, updated as FFmpeg processing progresses.
/// </summary>
internal class TrackFile : BaseEntity
{
    /// <summary>FK to the parent track. Also the UNIQUE constraint column.</summary>
    public Guid TrackId { get; set; }

    public TrackFileStatus Status { get; set; } = TrackFileStatus.Pending;

    /// <summary>Path to the full HLS playlist (.m3u8) — set when Status = Ready.</summary>
    public string? HlsPlaylistPath { get; set; }

    /// <summary>Path to the 30-second preview HLS playlist — set when Status = Ready.</summary>
    public string? PreviewPlaylistPath { get; set; }

    /// <summary>FFmpeg stderr output — set when Status = Failed.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Path to the uploaded raw audio file before processing.</summary>
    public string? RawFilePath { get; set; }

    /// <summary>Original uploaded file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Set when FFmpeg processing starts, used by the stuck track detector.</summary>
    public DateTime? ProcessingStartedAt { get; set; }

    // Navigation
    public Track Track { get; set; } = default!;
}
