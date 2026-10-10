using FluentAssertions;
using Microsoft.Extensions.Logging;
using MimeDetective;
using Moq;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage;
using SoundWave.SharedKernel.Storage.Services;
using SoundWave.SharedKernel.Validation;

namespace SoundWave.Identity.Tests.Services;

public class ImageUploadServiceTests
{
    private readonly FileMetadataValidator _validator;
    private readonly Mock<IObjectStorageService> _storageServiceMock = new();
    private readonly Mock<IFileMetadataRepository> _fileMetadataRepoMock = new();
    private readonly Mock<ILogger<ImageUploadService>> _loggerMock = new();
    private readonly ImageUploadService _service;

    public ImageUploadServiceTests()
    {
        var inspector = new ContentInspectorBuilder
        {
            Definitions =
            [
                ..MimeDetective.Definitions.DefaultDefinitions.FileTypes.Images.JPEG(),
                ..MimeDetective.Definitions.DefaultDefinitions.FileTypes.Images.PNG(),
                ..MimeDetective.Definitions.DefaultDefinitions.FileTypes.Images.WEBP()
            ]
        }.Build();

        _validator = new FileMetadataValidator(inspector);
        _service = new ImageUploadService(
            _validator,
            _storageServiceMock.Object,
            _fileMetadataRepoMock.Object,
            _loggerMock.Object);
    }

    private static MemoryStream CreateValidPngStream()
    {
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
        };
        return new MemoryStream(bytes);
    }

    [Fact]
    public async Task UploadImageAsync_WhenValid_ShouldUploadToS3AndReturnResult()
    {
        using var stream = CreateValidPngStream();
        _storageServiceMock
            .Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(k => $"http://localhost:9000/{k}");

        var result = await _service.UploadImageAsync(stream, "avatar.png", FileType.ProfilePic);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PublicUrl.Should().Contain("public/images/avatars/");
        result.Data.ContentType.Should().Be("image/png");

        _fileMetadataRepoMock.Verify(r => r.AddAsync(
            It.Is<FileMetadata>(m => m.Type == FileType.ProfilePic && m.Status == FileStatus.Available),
            It.IsAny<CancellationToken>()), Times.Once);

        _storageServiceMock.Verify(s => s.UploadAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            "image/png",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_WhenInvalid_ShouldReturnValidationFailure()
    {
        using var corruptStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });

        var result = await _service.UploadImageAsync(corruptStream, "avatar.png", FileType.ProfilePic);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.InvalidFileHeader);

        _storageServiceMock.Verify(s => s.UploadAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceImageAsync_WhenPreviousUrlExists_ShouldDeleteOldAndUploadNew()
    {
        using var stream = CreateValidPngStream();
        var oldKey = "public/images/avatars/old-file.webp";
        var oldUrl = $"http://localhost:9000/{oldKey}";

        var oldMetadata = FileMetadata.CreateAvailable(
            Guid.NewGuid(), oldKey, "old.webp", FileType.ProfilePic, "image/webp", 500);

        _fileMetadataRepoMock
            .Setup(r => r.GetByStorageKeyAsync(oldKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldMetadata);

        _storageServiceMock
            .Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(k => $"http://localhost:9000/{k}");

        var result = await _service.ReplaceImageAsync(
            stream, "avatar.png", FileType.ProfilePic, oldUrl);

        result.IsSuccess.Should().BeTrue();

        _storageServiceMock.Verify(s => s.DeleteAsync(oldKey, It.IsAny<CancellationToken>()), Times.Once);
        _fileMetadataRepoMock.Verify(r => r.UpdateAsync(
            It.Is<FileMetadata>(m => m.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_WhenStorageThrows_ShouldReturnStorageUnavailable()
    {
        using var stream = CreateValidPngStream();
        _storageServiceMock
            .Setup(s => s.UploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.UploadImageAsync(stream, "avatar.png", FileType.ProfilePic);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.StorageUnavailable);
    }
}
