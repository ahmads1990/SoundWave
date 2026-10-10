using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Storage.Services;

/// <summary>
/// Encapsulates verified image upload results including identifiers, storage keys, and public access URLs.
/// </summary>
public record ImageUploadResult(
    Guid FileId,
    string StorageKey,
    string PublicUrl,
    string ContentType,
    long SizeBytes);

/// <summary>
/// Domain service for handling image uploads, replacements, and deletions across all application modules.
/// Orchestrates metadata validation, S3 object storage persistence, and FileMetadata lifecycle tracking.
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Validates and uploads an image stream to S3, records FileMetadata as Available, and returns the public URL.
    /// </summary>
    Task<Result<SharedKernelError, ImageUploadResult>> UploadImageAsync(
        Stream stream,
        string fileName,
        FileType fileType,
        long maxSizeBytes = 5 * 1024 * 1024,
        SupportedMediaFormat allowedFormats = SupportedMediaFormat.AllImages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing image: uploads the new image, records FileMetadata, and soft-deletes the previous image.
    /// </summary>
    Task<Result<SharedKernelError, ImageUploadResult>> ReplaceImageAsync(
        Stream newImageStream,
        string fileName,
        FileType fileType,
        string? currentPublicUrlOrKey,
        long maxSizeBytes = 5 * 1024 * 1024,
        SupportedMediaFormat allowedFormats = SupportedMediaFormat.AllImages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an existing image metadata record and marks the underlying S3 object as expired.
    /// </summary>
    Task DeleteImageAsync(
        string currentPublicUrlOrKey,
        CancellationToken cancellationToken = default);
}
