using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage.Consumers;
using SoundWave.SharedKernel.Storage.Events;

namespace SoundWave.Catalog.Tests.Storage;

/// <summary>
/// Unit tests for <see cref="BucketNotificationConsumer"/> ObjectCreated status transitions.
/// Lives here because there is no SharedKernel test project; the track upload flow depends on it.
/// </summary>
public class BucketNotificationConsumerTests
{
    private const string StorageKey = "audio/raw/track/file.mp3";

    private readonly Mock<IFileMetadataRepository> _fileMetadataRepositoryMock = new();
    private readonly Mock<ConsumeContext<BucketNotification>> _contextMock = new();

    private BucketNotificationConsumer BuildConsumer()
        => new(_fileMetadataRepositoryMock.Object, Mock.Of<ILogger<BucketNotificationConsumer>>());

    private void SetupObjectCreatedMessage()
    {
        var message = new BucketNotification
        {
            Records =
            [
                new BucketNotificationRecord
                {
                    EventName = "s3:ObjectCreated:Put",
                    Storage = new BucketNotificationEntity
                    {
                        Object = new BucketNotificationObject { Key = StorageKey, Size = 2048, ETag = "etag-1" }
                    }
                }
            ]
        };

        _contextMock.Setup(c => c.Message).Returns(message);
        _contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
    }

    private FileMetadata SetupExistingMetadata(FileStatus status)
    {
        var metadata = FileMetadata.CreatePendingUpload(Guid.NewGuid(), StorageKey, "file.mp3", FileType.Track, "audio/mpeg", 1024);
        metadata.Status = status;

        _fileMetadataRepositoryMock
            .Setup(r => r.GetByStorageKeyAsync(StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        return metadata;
    }

    [Fact]
    public async Task Consume_ShouldMarkPendingValidationAndDispatch_WhenPendingUpload()
    {
        SetupObjectCreatedMessage();
        var metadata = SetupExistingMetadata(FileStatus.PendingUpload);

        await BuildConsumer().Consume(_contextMock.Object);

        metadata.Status.Should().Be(FileStatus.PendingValidation);
        metadata.SizeBytes.Should().Be(2048);
        _fileMetadataRepositoryMock.Verify(r => r.UpdateAsync(metadata, It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.Publish(
            It.Is<TrackReadyForValidationEvent>(e => e.FileId == metadata.Id && e.StorageKey == StorageKey),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(FileStatus.Available)]         // direct upload, validated server-side before the PUT
    [InlineData(FileStatus.PendingValidation)] // duplicate notification — must not bypass validation
    [InlineData(FileStatus.Processing)]
    [InlineData(FileStatus.Failed)]
    public async Task Consume_ShouldSkipWithoutStatusChange_WhenNotPendingUpload(FileStatus status)
    {
        SetupObjectCreatedMessage();
        var metadata = SetupExistingMetadata(status);

        await BuildConsumer().Consume(_contextMock.Object);

        metadata.Status.Should().Be(status);
        _fileMetadataRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.Publish(It.IsAny<TrackReadyForValidationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
