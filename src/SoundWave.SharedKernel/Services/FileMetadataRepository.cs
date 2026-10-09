using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Services;

/// <summary>
/// Implements SQL persistence and query operations for <see cref="FileMetadata"/> entities.
/// </summary>
public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly SharedKernelDbContext _dbContext;
    private readonly ILogger<FileMetadataRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadataRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for shared kernel tables.</param>
    /// <param name="logger">The logger instance.</param>
    public FileMetadataRepository(
        SharedKernelDbContext dbContext,
        ILogger<FileMetadataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileMetadata.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileMetadata?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileMetadata.FirstOrDefaultAsync(f => f.StorageKey == storageKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<FileMetadata>> GetStaleUploadsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileMetadata
            .Where(f => !f.IsDeleted && f.Status == FileStatus.PendingUpload && f.CreatedDate < cutoffUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<FileMetadata>> GetStaleValidationsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileMetadata
            .Where(f => !f.IsDeleted && f.Status == FileStatus.PendingValidation && f.CreatedDate < cutoffUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileMetadata> AddAsync(FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        _dbContext.FileMetadata.Add(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Persisted FileMetadata '{FileId}' with storage key '{StorageKey}' and status '{Status}'.", metadata.Id, metadata.StorageKey, metadata.Status);
        return metadata;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        _dbContext.FileMetadata.Update(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated FileMetadata '{FileId}' status to '{Status}'.", metadata.Id, metadata.Status);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.FileMetadata
            .Where(f => f.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} FileMetadata record(s) matching id '{FileId}'.", count, id);
    }

    /// <inheritdoc />
    public async Task DeleteByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.FileMetadata
            .Where(f => f.StorageKey == storageKey)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} FileMetadata record(s) matching storage key '{StorageKey}'.", count, storageKey);
    }
}
