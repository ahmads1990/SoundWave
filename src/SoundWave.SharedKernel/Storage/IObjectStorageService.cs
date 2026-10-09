namespace SoundWave.SharedKernel.Storage;

/// <summary>
/// Provides high-level object storage operations backed by S3-compatible providers (RustFS, MinIO, AWS S3).
/// Supports direct stream/byte uploads, presigned upload/download URLs, and metadata-driven lifecycle soft deletes.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Generates a presigned PUT URL allowing clients to stream uploads directly to S3 without routing payload bytes through the API.
    /// Signs <c>file-id</c> and optional custom metadata as required S3 metadata headers (<c>x-amz-meta-*</c>).
    /// </summary>
    /// <param name="fileId">The unique SQL file identifier associated with this upload.</param>
    /// <param name="key">The destination S3 object key.</param>
    /// <param name="expiry">The duration for which the presigned URL remains valid.</param>
    /// <param name="contentType">The expected MIME content type that the client must supply in the PUT request.</param>
    /// <param name="metadata">Optional additional metadata key-value pairs to attach to the object.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A signed S3 PUT URL ready for direct client upload.</returns>
    Task<string> GenerateUploadUrlAsync(
        Guid fileId,
        string key,
        TimeSpan expiry,
        string? contentType = null,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned GET URL for temporary, authenticated client-side download or streaming access to a private S3 object.
    /// </summary>
    /// <param name="key">The S3 object key to access.</param>
    /// <param name="expiry">The duration for which the download URL remains valid.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A signed S3 GET URL.</returns>
    Task<string> GenerateDownloadUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a stream directly to S3 with embedded metadata (<c>file-id</c> and optional custom metadata).
    /// Limited to streams up to the configured direct upload limit (default 250 MB).
    /// </summary>
    /// <param name="fileId">The unique SQL file identifier associated with this upload.</param>
    /// <param name="key">The destination S3 object key (e.g., generated via <see cref="StorageKeyGenerator"/>).</param>
    /// <param name="content">The readable data stream to upload.</param>
    /// <param name="contentType">The MIME content type of the object (e.g., "audio/mpeg", "image/webp").</param>
    /// <param name="metadata">Optional additional metadata key-value pairs to attach to the object.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="InvalidOperationException">Thrown when the stream length exceeds the direct upload size limit.</exception>
    Task UploadAsync(
        Guid fileId,
        string key,
        Stream content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads raw byte content directly to S3 with embedded metadata.
    /// </summary>
    /// <param name="fileId">The unique SQL file identifier associated with this upload.</param>
    /// <param name="key">The destination S3 object key.</param>
    /// <param name="content">The raw byte payload of the file.</param>
    /// <param name="contentType">The MIME content type of the object.</param>
    /// <param name="metadata">Optional additional metadata key-value pairs to attach to the object.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task UploadAsync(
        Guid fileId,
        string key,
        byte[] content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an object stream directly from object storage.
    /// </summary>
    /// <param name="key">The storage key of the target object.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The object content stream, or null if the object does not exist.</returns>
    Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an object in S3 by applying a lifecycle tag (<c>status=expired</c>).
    /// Tagged objects are automatically purged after the retention period via S3 lifecycle rules.
    /// </summary>
    /// <param name="key">The S3 object key to mark as expired.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an object exists at the specified storage key in the bucket.
    /// </summary>
    /// <param name="key">The storage key to verify.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the object exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs a fully qualified public HTTP(S) URL for accessing publicly accessible objects (e.g. avatars, cover images).
    /// </summary>
    /// <param name="key">The destination S3 object key.</param>
    /// <returns>The public URL pointing directly to the object.</returns>
    string GetPublicUrl(string key);

    /// <summary>
    /// Asynchronously returns the public direct-access URL for an object stored at the specified key.
    /// </summary>
    /// <param name="key">The destination S3 object key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task resolving to the fully qualified public URL pointing directly to the object.</returns>
    Task<string> GeneratePublicUrlAsync(string key, CancellationToken cancellationToken = default);
}
