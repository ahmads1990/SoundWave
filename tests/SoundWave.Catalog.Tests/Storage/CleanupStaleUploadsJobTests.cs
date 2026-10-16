using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Jobs;
using SoundWave.SharedKernel.Storage;

namespace SoundWave.Catalog.Tests.Storage;

/// <summary>
/// Unit tests for <see cref="CleanupStaleUploadsJob"/>.
/// Lives here because there is no SharedKernel test project; the track upload flow depends on it.
/// </summary>
public class CleanupStaleUploadsJobTests
{
    private readonly Mock<IFileMetadataRepository> _fileMetadataRepositoryMock = new();
    private readonly Mock<IObjectStorageService> _storageServiceMock = new();

    private CleanupStaleUploadsJob BuildJob()
        => new(
            _fileMetadataRepositoryMock.Object,
            _storageServiceMock.Object,
            Options.Create(new CleanupStaleUploadsConfig()),
            Mock.Of<ILogger<CleanupStaleUploadsJob>>());

    private FileMetadata SetupStaleUpload(string storageKey)
    {
        var metadata = FileMetadata.CreatePendingUpload(Guid.NewGuid(), storageKey, "file.mp3", FileType.Track, "audio/mpeg", 1024);

        _fileMetadataRepositoryMock
            .Setup(r => r.GetStaleUploadsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([metadata]);

        return metadata;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldScavenge_WhenObjectWasNeverUploaded()
    {
        var metadata = SetupStaleUpload("audio/raw/track/abandoned.mp3");
        _storageServiceMock.Setup(s => s.ExistsAsync(metadata.StorageKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await BuildJob().ExecuteAsync();

        metadata.Status.Should().Be(FileStatus.Failed);
        metadata.IsDeleted.Should().BeTrue();
        _storageServiceMock.Verify(s => s.DeleteAsync(metadata.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
        _fileMetadataRepositoryMock.Verify(r => r.UpdateAsync(metadata, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRescueToPendingValidation_WhenObjectExistsButNotificationWasMissed()
    {
        var metadata = SetupStaleUpload("audio/raw/track/uploaded.mp3");
        _storageServiceMock.Setup(s => s.ExistsAsync(metadata.StorageKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await BuildJob().ExecuteAsync();

        metadata.Status.Should().Be(FileStatus.PendingValidation);
        metadata.IsDeleted.Should().BeFalse();
        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fileMetadataRepositoryMock.Verify(r => r.UpdateAsync(metadata, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLeaveRecordUntouched_WhenStorageCheckFails()
    {
        var metadata = SetupStaleUpload("audio/raw/track/unknown.mp3");
        _storageServiceMock
            .Setup(s => s.ExistsAsync(metadata.StorageKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("storage unreachable"));

        await BuildJob().ExecuteAsync();

        metadata.Status.Should().Be(FileStatus.PendingUpload);
        metadata.IsDeleted.Should().BeFalse();
        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fileMetadataRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
