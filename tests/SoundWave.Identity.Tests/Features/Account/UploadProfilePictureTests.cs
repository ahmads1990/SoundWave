using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.Account.UploadProfilePicture;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Identity.Tests.Features.Account;

public class UploadProfilePictureTests : IdentityIntegrationTestBase
{
    private readonly Mock<IImageUploadService> _imageUploadServiceMock = new();
    private readonly Mock<ILogger<UploadProfilePictureCommandHandler>> _loggerMock = new();

    private UploadProfilePictureCommandHandler BuildHandler()
    {
        var repo = new IdentityRepository<UserProfile>(DbContext);
        return new UploadProfilePictureCommandHandler(repo, _imageUploadServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ShouldReturnUserNotFound()
    {
        var handler = BuildHandler();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadProfilePictureCommand(Guid.NewGuid(), stream, "avatar.png", stream.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
    }

    [Fact]
    public async Task Handle_WhenImageServiceFails_ShouldReturnFileValidationFailed()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "alice@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = UserRole.Listener
        };
        await SeedAsync(user);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Alice",
            LastName = "Smith",
            DisplayName = "alicesmith"
        };
        await SeedAsync(profile);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), FileType.ProfilePic, It.IsAny<string?>(),
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Failure(
                SharedKernelError.InvalidFileHeader, "Bad image"));

        var handler = BuildHandler();
        var command = new UploadProfilePictureCommand(userId, stream, "corrupt.png", stream.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.FileValidationFailed);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateProfilePicUrlAndReturnSuccess()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "bob@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = UserRole.Listener
        };
        await SeedAsync(user);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Bob",
            LastName = "Jones",
            DisplayName = "bobjones",
            ProfilePicUrl = "http://old-avatar.webp"
        };
        await SeedAsync(profile);

        var newFileId = Guid.NewGuid();
        var newUrl = "http://storage.soundwave/public/images/avatars/new.webp";

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), "new.png", FileType.ProfilePic, "http://old-avatar.webp",
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Success(
                new ImageUploadResult(newFileId, "key", newUrl, "image/webp", 1024)));

        var handler = BuildHandler();
        var command = new UploadProfilePictureCommand(userId, stream, "new.png", stream.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileId.Should().Be(newFileId);
        result.Data.Url.Should().Be(newUrl);

        var updated = await DbContext.UserProfiles.FindAsync(profile.Id);
        updated.Should().NotBeNull();
        updated!.ProfilePicUrl.Should().Be(newUrl);
    }
}
