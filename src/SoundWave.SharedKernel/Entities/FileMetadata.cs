using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Entities;

/// <summary>
/// Domain entity tracking an uploaded file's metadata, verification lifecycle, and storage key.
/// Encapsulates state transitions and factory methods for both presigned and direct upload workflows.
/// </summary>
public class FileMetadata : BaseEntity
{
    /// <summary>
    /// Gets or sets the original or sanitized file name provided during upload.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object storage key path (e.g. public/images/avatars/{fileId}.webp or audio/raw/{trackId}.mp3).
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain classification or category of the file.
    /// </summary>
    public FileType Type { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the file (e.g., "audio/mpeg", "image/webp").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the uploaded file payload in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the current lifecycle status of the file upload.
    /// </summary>
    public FileStatus Status { get; set; } = FileStatus.PendingUpload;

    /// <summary>
    /// Gets or sets the entity tag (ETag) or checksum assigned by object storage.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the failure description if validation or storage processing failed.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the file was validated and marked available for consumption.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadata"/> class.
    /// Parameterless constructor required by EF Core.
    /// </summary>
    public FileMetadata()
    {
    }

    /// <summary>
    /// Factory method for initiating a pending upload session (e.g., prior to generating a presigned PUT URL).
    /// </summary>
    /// <param name="fileId">The unique file identifier.</param>
    /// <param name="storageKey">The destination storage key.</param>
    /// <param name="fileName">The original or sanitized file name.</param>
    /// <param name="fileType">The classification type of the file.</param>
    /// <param name="contentType">The expected MIME content type.</param>
    /// <param name="expectedSizeBytes">Optional anticipated file size in bytes.</param>
    /// <returns>A new <see cref="FileMetadata"/> instance in <see cref="FileStatus.PendingUpload"/> state.</returns>
    public static FileMetadata CreatePendingUpload(
        Guid fileId,
        string storageKey,
        string fileName,
        FileType fileType,
        string contentType,
        long expectedSizeBytes = 0)
    {
        var now = DateTime.UtcNow;
        return new FileMetadata
        {
            Id = fileId,
            StorageKey = storageKey,
            FileName = fileName,
            Type = fileType,
            ContentType = contentType,
            SizeBytes = expectedSizeBytes,
            Status = FileStatus.PendingUpload,
            CreatedDate = now,
            UpdatedDate = now,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Factory method for server-side direct uploads that are validated and available immediately upon creation.
    /// </summary>
    /// <param name="fileId">The unique file identifier.</param>
    /// <param name="storageKey">The destination storage key.</param>
    /// <param name="fileName">The original or sanitized file name.</param>
    /// <param name="fileType">The classification type of the file.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="sizeBytes">The verified file size in bytes.</param>
    /// <param name="eTag">Optional ETag returned from object storage.</param>
    /// <returns>A new <see cref="FileMetadata"/> instance in <see cref="FileStatus.Available"/> state.</returns>
    public static FileMetadata CreateAvailable(
        Guid fileId,
        string storageKey,
        string fileName,
        FileType fileType,
        string contentType,
        long sizeBytes,
        string? eTag = null)
    {
        var now = DateTime.UtcNow;
        return new FileMetadata
        {
            Id = fileId,
            StorageKey = storageKey,
            FileName = fileName,
            Type = fileType,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            ETag = eTag,
            Status = FileStatus.Available,
            CreatedDate = now,
            ConfirmedAt = now,
            UpdatedDate = now,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Transitions status to <see cref="FileStatus.PendingValidation"/> once binary upload completes in object storage.
    /// </summary>
    /// <param name="actualSizeBytes">Optional verified size in bytes from storage notification.</param>
    /// <param name="eTag">Optional ETag checksum from storage notification.</param>
    public void MarkPendingValidation(long? actualSizeBytes = null, string? eTag = null)
    {
        Status = FileStatus.PendingValidation;
        UpdatedDate = DateTime.UtcNow;
        if (actualSizeBytes.HasValue) SizeBytes = actualSizeBytes.Value;
        if (!string.IsNullOrWhiteSpace(eTag)) ETag = eTag;
    }

    /// <summary>
    /// Transitions status to <see cref="FileStatus.Available"/> once file validation succeeds.
    /// </summary>
    /// <param name="actualSizeBytes">Optional verified size in bytes.</param>
    /// <param name="eTag">Optional verified ETag.</param>
    public void MarkAvailable(long? actualSizeBytes = null, string? eTag = null)
    {
        Status = FileStatus.Available;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedDate = DateTime.UtcNow;
        FailureReason = null;
        if (actualSizeBytes.HasValue) SizeBytes = actualSizeBytes.Value;
        if (!string.IsNullOrWhiteSpace(eTag)) ETag = eTag;
    }

    /// <summary>
    /// Transitions status to <see cref="FileStatus.Failed"/> when upload or async validation fails.
    /// </summary>
    /// <param name="reason">The explanation for the failure.</param>
    public void MarkFailed(string reason)
    {
        Status = FileStatus.Failed;
        FailureReason = reason;
        UpdatedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the file record as soft-deleted and records the update timestamp.
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedDate = DateTime.UtcNow;
    }
}
