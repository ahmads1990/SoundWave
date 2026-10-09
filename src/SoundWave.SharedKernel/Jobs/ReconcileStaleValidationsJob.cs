using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Interfaces.Jobs;
using SoundWave.SharedKernel.Storage.Events;

namespace SoundWave.SharedKernel.Jobs;

/// <summary>
/// Hangfire recurring job that re-dispatches validation events for uploads stuck in <see cref="FileStatus.PendingValidation"/>.
/// </summary>
public class ReconcileStaleValidationsJob : IReconcileStaleValidationsJob
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IBus _bus;
    private readonly ReconcileStaleValidationsConfig _config;
    private readonly ILogger<ReconcileStaleValidationsJob> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconcileStaleValidationsJob"/> class.
    /// </summary>
    /// <param name="fileMetadataRepository">The file metadata repository.</param>
    /// <param name="bus">The MassTransit message bus.</param>
    /// <param name="configOptions">The reconciliation job configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ReconcileStaleValidationsJob(
        IFileMetadataRepository fileMetadataRepository,
        IBus bus,
        IOptions<ReconcileStaleValidationsConfig> configOptions,
        ILogger<ReconcileStaleValidationsJob> logger)
    {
        _fileMetadataRepository = fileMetadataRepository;
        _bus = bus;
        _config = configOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoffWindow = _config.GetCutoffTimeSpan();
        var cutoffUtc = DateTime.UtcNow - cutoffWindow;

        _logger.LogInformation("Starting ReconcileStaleValidationsJob with cutoff threshold: {CutoffUtc} (older than {Window})", cutoffUtc, cutoffWindow);

        var stuckRecords = await _fileMetadataRepository.GetStaleValidationsAsync(cutoffUtc, cancellationToken);
        if (stuckRecords.Count == 0)
        {
            _logger.LogInformation("ReconcileStaleValidationsJob completed: no stuck validation records found.");
            return;
        }

        _logger.LogInformation("Found {Count} stuck validation record(s) to reconcile.", stuckRecords.Count);

        var reconciledCount = 0;
        var errorCount = 0;

        foreach (var metadata in stuckRecords)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("ReconcileStaleValidationsJob cancelled early after processing {Count} record(s).", reconciledCount);
                break;
            }

            try
            {
                switch (metadata.Type)
                {
                    case FileType.Track:
                        await _bus.Publish(new TrackReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                        break;
                    case FileType.ProfilePic:
                        await _bus.Publish(new ProfilePicReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                        break;
                    case FileType.AlbumCover:
                        await _bus.Publish(new AlbumCoverReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                        break;
                    case FileType.ProfileBanner:
                        await _bus.Publish(new ProfileBannerReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                        break;
                    case FileType.PlaylistCover:
                        await _bus.Publish(new PlaylistCoverReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                        break;
                    case FileType.Video:
                        await _bus.Publish(new VideoReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                        break;
                }

                reconciledCount++;
                _logger.LogInformation("Re-dispatched validation event for FileMetadata '{FileId}' (Type: {FileType}, StorageKey: '{StorageKey}').", metadata.Id, metadata.Type, metadata.StorageKey);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to re-dispatch validation event for FileMetadata '{FileId}'.", metadata.Id);
            }
        }

        _logger.LogInformation("ReconcileStaleValidationsJob finished: {ReconciledCount} event(s) re-dispatched, {ErrorCount} error(s).", reconciledCount, errorCount);
    }
}
