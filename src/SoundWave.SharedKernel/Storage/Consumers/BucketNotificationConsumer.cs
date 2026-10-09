using System.Net;
using MassTransit;
using Microsoft.Extensions.Logging;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage.Events;

namespace SoundWave.SharedKernel.Storage.Consumers;

/// <summary>
/// Consumes object storage bucket notifications (ObjectCreated, ObjectRemoved) from RabbitMQ.
/// Updates <see cref="FileMetadata"/> status in SQL and fires type-specific validation events.
/// </summary>
public class BucketNotificationConsumer : IConsumer<BucketNotification>
{
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly ILogger<BucketNotificationConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BucketNotificationConsumer"/> class.
    /// </summary>
    /// <param name="fileMetadataRepository">The file metadata repository.</param>
    /// <param name="logger">The logger instance.</param>
    public BucketNotificationConsumer(
        IFileMetadataRepository fileMetadataRepository,
        ILogger<BucketNotificationConsumer> logger)
    {
        _fileMetadataRepository = fileMetadataRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<BucketNotification> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        if (message.Records.Count == 0) return;

        foreach (var record in message.Records)
        {
            var rawKey = record.Storage?.Object?.Key;
            if (string.IsNullOrWhiteSpace(rawKey)) continue;

            var storageKey = WebUtility.UrlDecode(rawKey);
            var eventName = record.EventName ?? string.Empty;

            if (eventName.Contains("ObjectCreated", StringComparison.OrdinalIgnoreCase))
            {
                await HandleObjectCreatedAsync(storageKey, record, context, cancellationToken);
            }
            else if (eventName.Contains("ObjectRemoved", StringComparison.OrdinalIgnoreCase))
            {
                await HandleObjectRemovedAsync(storageKey, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles S3 ObjectCreated events by transitioning FileMetadata status and publishing validation events.
    /// </summary>
    private async Task HandleObjectCreatedAsync(string storageKey, BucketNotificationRecord record, ConsumeContext context, CancellationToken cancellationToken)
    {
        var metadata = await _fileMetadataRepository.GetByStorageKeyAsync(storageKey, cancellationToken);
        if (metadata == null)
        {
            _logger.LogWarning("FileMetadata not found in database for created storage object '{StorageKey}'.", storageKey);
            return;
        }

        if (metadata.Status == FileStatus.PendingUpload)
        {
            metadata.MarkPendingValidation(record.Storage.Object.Size, record.Storage.Object.ETag);
            await _fileMetadataRepository.UpdateAsync(metadata, cancellationToken);
            _logger.LogInformation("FileMetadata '{FileId}' transitioned from PendingUpload to PendingValidation for object '{StorageKey}'.", metadata.Id, storageKey);

            await DispatchValidationEventAsync(metadata, context, cancellationToken);
        }
        else if (metadata.Status == FileStatus.PendingValidation)
        {
            metadata.MarkAvailable(record.Storage.Object.Size, record.Storage.Object.ETag);
            await _fileMetadataRepository.UpdateAsync(metadata, cancellationToken);
            _logger.LogInformation("FileMetadata '{FileId}' transitioned from PendingValidation to Available (direct upload pre-validated) for object '{StorageKey}'.", metadata.Id, storageKey);
        }
        else
        {
            _logger.LogDebug("FileMetadata '{FileId}' already in status '{Status}', skipping transition for object '{StorageKey}'.", metadata.Id, metadata.Status, storageKey);
        }
    }

    /// <summary>
    /// Publishes a type-specific validation event to trigger downstream validation consumers.
    /// </summary>
    private static async Task DispatchValidationEventAsync(FileMetadata metadata, ConsumeContext context, CancellationToken cancellationToken)
    {
        switch (metadata.Type)
        {
            case FileType.Track:
                await context.Publish(new TrackReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                break;
            case FileType.ProfilePic:
                await context.Publish(new ProfilePicReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                break;
            case FileType.AlbumCover:
                await context.Publish(new AlbumCoverReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                break;
            case FileType.ProfileBanner:
                await context.Publish(new ProfileBannerReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                break;
            case FileType.PlaylistCover:
                await context.Publish(new PlaylistCoverReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                break;
            case FileType.Video:
                await context.Publish(new VideoReadyForValidationEvent(metadata.Id, metadata.StorageKey), cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Handles S3 ObjectRemoved events by deleting FileMetadata from SQL storage.
    /// </summary>
    private async Task HandleObjectRemovedAsync(string storageKey, CancellationToken cancellationToken)
    {
        await _fileMetadataRepository.DeleteByStorageKeyAsync(storageKey, cancellationToken);
        _logger.LogInformation("Deleted FileMetadata for expired/removed storage object '{StorageKey}'.", storageKey);
    }
}
