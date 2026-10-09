using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Interfaces.Jobs;
using SoundWave.SharedKernel.Storage;

namespace SoundWave.SharedKernel.Jobs;

/// <summary>
/// Hangfire recurring job that scavenges abandoned or timed-out pending uploads.
/// Marks stale records as deleted and tags corresponding S3 objects as expired.
/// </summary>
public class CleanupStaleUploadsJob : ICleanupStaleUploadsJob
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IObjectStorageService _storageService;
    private readonly CleanupStaleUploadsConfig _config;
    private readonly ILogger<CleanupStaleUploadsJob> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupStaleUploadsJob"/> class.
    /// </summary>
    /// <param name="fileMetadataRepository">The file metadata repository.</param>
    /// <param name="storageService">The object storage service.</param>
    /// <param name="configOptions">The cleanup job configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public CleanupStaleUploadsJob(
        IFileMetadataRepository fileMetadataRepository,
        IObjectStorageService storageService,
        IOptions<CleanupStaleUploadsConfig> configOptions,
        ILogger<CleanupStaleUploadsJob> logger)
    {
        _fileMetadataRepository = fileMetadataRepository;
        _storageService = storageService;
        _config = configOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoffWindow = _config.GetCutoffTimeSpan();
        var cutoffUtc = DateTime.UtcNow - cutoffWindow;

        _logger.LogInformation("Starting CleanupStaleUploadsJob with cutoff threshold: {CutoffUtc} (older than {Window})", cutoffUtc, cutoffWindow);

        var staleRecords = await _fileMetadataRepository.GetStaleUploadsAsync(cutoffUtc, cancellationToken);
        if (staleRecords.Count == 0)
        {
            _logger.LogInformation("CleanupStaleUploadsJob completed: no stale upload records found.");
            return;
        }

        _logger.LogInformation("Found {Count} stale upload record(s) to scavenge.", staleRecords.Count);

        var purgedCount = 0;
        var errorCount = 0;

        foreach (var metadata in staleRecords)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("CleanupStaleUploadsJob cancelled early after processing {PurgedCount} record(s).", purgedCount);
                break;
            }

            try
            {
                // 1. Tag or delete object from S3 storage if it exists
                await _storageService.DeleteAsync(metadata.StorageKey, cancellationToken);

                // 2. Mark database record as deleted and record failure reason
                metadata.MarkFailed($"Upload abandoned by client; scavenged after {cutoffWindow}.");
                metadata.MarkAsDeleted();
                await _fileMetadataRepository.UpdateAsync(metadata, cancellationToken);

                purgedCount++;
                _logger.LogInformation("Scavenged stale upload for FileMetadata '{FileId}' (StorageKey: '{StorageKey}').", metadata.Id, metadata.StorageKey);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to scavenge stale upload for FileMetadata '{FileId}' (StorageKey: '{StorageKey}').", metadata.Id, metadata.StorageKey);
            }
        }

        _logger.LogInformation("CleanupStaleUploadsJob finished: {PurgedCount} record(s) cleaned up, {ErrorCount} error(s).", purgedCount, errorCount);
    }
}
