using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Represents a single track within an album.
/// </summary>
internal class Track : BaseEntity
{
    public Guid AlbumId { get; set; }
    public string Title { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }
    public int TrackNumber { get; set; }

    /// <summary>
    /// Denormalized play count. Authoritative value lives in Redis and is periodically
    /// flushed here by the play count flush BackgroundService.
    /// </summary>
    public long PlayCount { get; set; }

    /// <summary>Denormalized like count — incremented/decremented by LikeTrackCommand.</summary>
    public int LikeCount { get; set; }

    // Navigation
    public Album Album { get; set; } = default!;
    public TrackFile? TrackFile { get; set; }
    public ICollection<TrackArtist> TrackArtists { get; set; } = [];
    public ICollection<TrackGenre> TrackGenres { get; set; } = [];

    /// <summary>
    /// Initializes or resets the track's <see cref="TrackFile"/> for a new pending audio upload,
    /// clearing any prior processing paths, timestamps, or failure details.
    /// </summary>
    public void SetPendingUpload(Guid fileId, string storageKey, long fileSizeBytes)
    {
        TrackFile ??= new TrackFile { TrackId = Id };
        TrackFile.FileId = fileId;
        TrackFile.RawFilePath = storageKey;
        TrackFile.FileSizeBytes = fileSizeBytes;
        TrackFile.Status = FileStatus.PendingUpload;
        TrackFile.FailureReason = null;
        TrackFile.HlsPlaylistPath = null;
        TrackFile.PreviewPlaylistPath = null;
        TrackFile.ProcessingStartedAt = null;
    }
}
