using SoundWave.SharedKernel.Entities;

namespace SoundWave.SharedKernel.Interfaces;

/// <summary>
/// Repository interface for persisting, querying, and managing <see cref="FileMetadata"/> records in SQL storage.
/// </summary>
public interface IFileMetadataRepository
{
    /// <summary>
    /// Retrieves a file metadata record by its unique database identifier.
    /// </summary>
    /// <param name="id">The unique file identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="FileMetadata"/>, or null if not found.</returns>
    Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file metadata record by its object storage key path.
    /// </summary>
    /// <param name="storageKey">The S3 object key (e.g., generated via StorageKeyGenerator).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="FileMetadata"/>, or null if not found.</returns>
    Task<FileMetadata?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves stale pending upload records created before the specified cutoff timestamp.
    /// Leverages composite index (Status, CreatedDate).
    /// </summary>
    /// <param name="cutoffUtc">The UTC cutoff timestamp.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of abandoned or stale <see cref="FileMetadata"/> records.</returns>
    Task<List<FileMetadata>> GetStaleUploadsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves stale records stuck in pending validation created before the specified cutoff timestamp.
    /// Leverages composite index (Status, CreatedDate).
    /// </summary>
    /// <param name="cutoffUtc">The UTC cutoff timestamp.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of stuck <see cref="FileMetadata"/> records requiring validation reconciliation.</returns>
    Task<List<FileMetadata>> GetStaleValidationsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds and persists a new file metadata record in the database.
    /// </summary>
    /// <param name="metadata">The file metadata entity to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The persisted <see cref="FileMetadata"/> instance.</returns>
    Task<FileMetadata> AddAsync(FileMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing file metadata record in the database.
    /// </summary>
    /// <param name="metadata">The file metadata entity with modified state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task UpdateAsync(FileMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a file metadata record matching the specified identifier from SQL storage.
    /// </summary>
    /// <param name="id">The unique file identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a file metadata record matching the specified storage key from SQL storage.
    /// </summary>
    /// <param name="storageKey">The S3 object key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DeleteByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);
}
