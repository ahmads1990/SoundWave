using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Storage;

namespace SoundWave.Identity.Tests.Services;

public class FileMetadataTests
{
    [Fact]
    public void CreatePendingUpload_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var storageKey = StorageKeyGenerator.ProfilePicture(fileId);
        var fileName = "avatar.png";
        var fileType = FileType.ProfilePic;
        var contentType = "image/png";
        var expectedSize = 1024L;

        // Act
        var metadata = FileMetadata.CreatePendingUpload(
            fileId, storageKey, fileName, fileType, contentType, expectedSize);

        // Assert
        Assert.Equal(fileId, metadata.Id);
        Assert.Equal(storageKey, metadata.StorageKey);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(fileType, metadata.Type);
        Assert.Equal(contentType, metadata.ContentType);
        Assert.Equal(expectedSize, metadata.SizeBytes);
        Assert.Equal(FileStatus.PendingUpload, metadata.Status);
        Assert.False(metadata.IsDeleted);
        Assert.Null(metadata.ConfirmedAt);
        Assert.Null(metadata.FailureReason);
        Assert.True(metadata.CreatedDate <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateAvailable_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var storageKey = StorageKeyGenerator.ProfilePicture(fileId);
        var fileName = "avatar.webp";
        var fileType = FileType.ProfilePic;
        var contentType = "image/webp";
        var sizeBytes = 2048L;
        var eTag = "\"test-etag-123\"";

        // Act
        var metadata = FileMetadata.CreateAvailable(
            fileId, storageKey, fileName, fileType, contentType, sizeBytes, eTag);

        // Assert
        Assert.Equal(fileId, metadata.Id);
        Assert.Equal(storageKey, metadata.StorageKey);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(fileType, metadata.Type);
        Assert.Equal(contentType, metadata.ContentType);
        Assert.Equal(sizeBytes, metadata.SizeBytes);
        Assert.Equal(eTag, metadata.ETag);
        Assert.Equal(FileStatus.Available, metadata.Status);
        Assert.False(metadata.IsDeleted);
        Assert.NotNull(metadata.ConfirmedAt);
        Assert.Null(metadata.FailureReason);
    }

    [Fact]
    public void MarkPendingValidation_ShouldUpdateStatusAndDetails()
    {
        // Arrange
        var metadata = FileMetadata.CreatePendingUpload(
            Guid.NewGuid(), "key", "test.mp3", FileType.Track, "audio/mpeg");

        // Act
        metadata.MarkPendingValidation(5000L, "\"new-etag\"");

        // Assert
        Assert.Equal(FileStatus.PendingValidation, metadata.Status);
        Assert.Equal(5000L, metadata.SizeBytes);
        Assert.Equal("\"new-etag\"", metadata.ETag);
        Assert.NotNull(metadata.UpdatedDate);
    }

    [Fact]
    public void MarkAvailable_ShouldUpdateStatusConfirmedAtAndClearFailure()
    {
        // Arrange
        var metadata = FileMetadata.CreatePendingUpload(
            Guid.NewGuid(), "key", "test.mp3", FileType.Track, "audio/mpeg");
        metadata.MarkFailed("temporary error");

        // Act
        metadata.MarkAvailable(6000L, "\"confirmed-etag\"");

        // Assert
        Assert.Equal(FileStatus.Available, metadata.Status);
        Assert.Equal(6000L, metadata.SizeBytes);
        Assert.Equal("\"confirmed-etag\"", metadata.ETag);
        Assert.Null(metadata.FailureReason);
        Assert.NotNull(metadata.ConfirmedAt);
    }

    [Fact]
    public void MarkFailed_ShouldRecordFailureReason()
    {
        // Arrange
        var metadata = FileMetadata.CreatePendingUpload(
            Guid.NewGuid(), "key", "test.mp3", FileType.Track, "audio/mpeg");

        // Act
        metadata.MarkFailed("Corrupt audio header.");

        // Assert
        Assert.Equal(FileStatus.Failed, metadata.Status);
        Assert.Equal("Corrupt audio header.", metadata.FailureReason);
        Assert.NotNull(metadata.UpdatedDate);
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetIsDeletedTrue()
    {
        // Arrange
        var metadata = FileMetadata.CreateAvailable(
            Guid.NewGuid(), "key", "test.webp", FileType.ProfilePic, "image/webp", 100);

        // Act
        metadata.MarkAsDeleted();

        // Assert
        Assert.True(metadata.IsDeleted);
        Assert.NotNull(metadata.UpdatedDate);
    }
}
