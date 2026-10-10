using FluentAssertions;
using MimeDetective;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Validation;

namespace SoundWave.Identity.Tests.Services;

public class FileMetadataValidatorTests
{
    private readonly FileMetadataValidator _validator;

    public FileMetadataValidatorTests()
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
    }

    private static MemoryStream CreateJpegStream()
    {
        // Standard JPEG header: FF D8 FF E0 00 10 4A 46 49 46 00 01
        var bytes = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x60, 0x00, 0x60, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43
        };
        return new MemoryStream(bytes);
    }

    private static MemoryStream CreatePngStream()
    {
        // Standard PNG header: 89 50 4E 47 0D 0A 1A 0A
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
        };
        return new MemoryStream(bytes);
    }

    private static MemoryStream CreateWebpStream()
    {
        // Standard WebP RIFF header: "RIFF" + size + "WEBPVP8 "
        var bytes = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20
        };
        return new MemoryStream(bytes);
    }

    [Fact]
    public void ValidateImage_WhenValidJpeg_ShouldSucceed()
    {
        using var stream = CreateJpegStream();

        var result = _validator.ValidateImage(stream, "photo.jpg");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ContentType.Should().Be("image/jpeg");
        result.Data.Format.Should().Be(SupportedMediaFormat.Jpeg);
        result.Data.Extension.Should().Be(".jpg");
    }

    [Fact]
    public void ValidateImage_WhenValidPng_ShouldSucceed()
    {
        using var stream = CreatePngStream();

        var result = _validator.ValidateImage(stream, "avatar.png");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ContentType.Should().Be("image/png");
        result.Data.Format.Should().Be(SupportedMediaFormat.Png);
        result.Data.Extension.Should().Be(".png");
    }

    [Fact]
    public void ValidateImage_WhenValidWebp_ShouldSucceed()
    {
        using var stream = CreateWebpStream();

        var result = _validator.ValidateImage(stream, "cover.webp");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ContentType.Should().Be("image/webp");
        result.Data.Format.Should().Be(SupportedMediaFormat.Webp);
        result.Data.Extension.Should().Be(".webp");
    }

    [Fact]
    public void Validate_WhenStreamIsEmpty_ShouldReturnEmptyFile()
    {
        using var stream = new MemoryStream();

        var result = _validator.Validate(stream, "empty.png", 5 * 1024 * 1024, SupportedMediaFormat.AllImages);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.EmptyFile);
    }

    [Fact]
    public void Validate_WhenFileExceedsMaxSizeBytes_ShouldReturnFileExceedsMaxSize()
    {
        using var stream = CreateJpegStream();

        var result = _validator.Validate(stream, "test.jpg", maxSizeBytes: 10, SupportedMediaFormat.AllImages);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.FileExceedsMaxSize);
    }

    [Fact]
    public void Validate_WhenExtensionMissing_ShouldReturnUnsupportedFileExtension()
    {
        using var stream = CreateJpegStream();

        var result = _validator.Validate(stream, "testfile", 5 * 1024 * 1024, SupportedMediaFormat.AllImages);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.UnsupportedFileExtension);
    }

    [Fact]
    public void Validate_WhenFormatRestrictedByCaller_ShouldRejectDisallowedFormat()
    {
        using var stream = CreateJpegStream();

        // Caller only allows PNG or WebP
        var result = _validator.Validate(
            stream, "photo.jpg", 5 * 1024 * 1024, SupportedMediaFormat.Png | SupportedMediaFormat.Webp);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.UnsupportedFileExtension);
    }

    [Fact]
    public void Validate_WhenStreamBytesAreCorrupt_ShouldReturnInvalidFileHeader()
    {
        using var stream = new MemoryStream(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 });

        var result = _validator.ValidateImage(stream, "test.png");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SharedKernelError.InvalidFileHeader);
    }
}
