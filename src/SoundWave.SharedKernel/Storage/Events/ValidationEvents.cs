namespace SoundWave.SharedKernel.Storage.Events;

/// <summary>
/// Common interface for all media validation events.
/// </summary>
public interface IFileValidationEvent
{
    /// <summary>The unique database identifier of the file.</summary>
    Guid FileId { get; }

    /// <summary>The S3 object key of the file in storage.</summary>
    string StorageKey { get; }
}

/// <summary>
/// Event published when an audio track file has completed upload and is ready for validation.
/// </summary>
/// <param name="FileId">The unique database identifier of the file.</param>
/// <param name="StorageKey">The S3 object key of the audio track.</param>
public record TrackReadyForValidationEvent(Guid FileId, string StorageKey) : IFileValidationEvent;

/// <summary>
/// Event published when a user avatar image has completed upload and is ready for validation.
/// </summary>
/// <param name="FileId">The unique database identifier of the file.</param>
/// <param name="StorageKey">The S3 object key of the avatar image.</param>
public record ProfilePicReadyForValidationEvent(Guid FileId, string StorageKey) : IFileValidationEvent;

/// <summary>
/// Event published when an album cover artwork has completed upload and is ready for validation.
/// </summary>
/// <param name="FileId">The unique database identifier of the file.</param>
/// <param name="StorageKey">The S3 object key of the album cover image.</param>
public record AlbumCoverReadyForValidationEvent(Guid FileId, string StorageKey) : IFileValidationEvent;

/// <summary>
/// Event published when a user profile banner image has completed upload and is ready for validation.
/// </summary>
/// <param name="FileId">The unique database identifier of the file.</param>
/// <param name="StorageKey">The S3 object key of the profile banner image.</param>
public record ProfileBannerReadyForValidationEvent(Guid FileId, string StorageKey) : IFileValidationEvent;

/// <summary>
/// Event published when a playlist cover image has completed upload and is ready for validation.
/// </summary>
/// <param name="FileId">The unique database identifier of the file.</param>
/// <param name="StorageKey">The S3 object key of the playlist cover image.</param>
public record PlaylistCoverReadyForValidationEvent(Guid FileId, string StorageKey) : IFileValidationEvent;

/// <summary>
/// Event published when a video file has completed upload and is ready for validation.
/// </summary>
/// <param name="FileId">The unique database identifier of the file.</param>
/// <param name="StorageKey">The S3 object key of the video.</param>
public record VideoReadyForValidationEvent(Guid FileId, string StorageKey) : IFileValidationEvent;
