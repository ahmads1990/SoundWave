using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.Account.UploadProfileBanner;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Identity.Tests.Features.Account;

public class UploadProfileBannerTests : IdentityIntegrationTestBase
{
    private readonly Mock<IImageUploadService> _imageUploadServiceMock = new();
    private readonly Mock<ILogger<UploadProfileBannerCommandHandler>> _loggerMock = new();

    private UploadProfileBannerCommandHandler BuildHandler()
    {
        var repo = new IdentityRepository<UserProfile>(DbContext);
        return new UploadProfileBannerCommandHandler(repo, _imageUploadServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ShouldReturnUserNotFound()
    {
        var handler = BuildHandler();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadProfileBannerCommand(Guid.NewGuid(), stream, "banner.png", stream.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateCoverImageUrlAndReturnSuccess()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "charlie@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = UserRole.Listener
        };
        await SeedAsync(user);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Charlie",
            LastName = "Brown",
            DisplayName = "cbrown",
            CoverImageUrl = "http://old-banner.webp"
        };
        await SeedAsync(profile);

        var newFileId = Guid.NewGuid();
        var newUrl = "http://storage.soundwave/public/images/banners/profiles/new.webp";

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), "banner.jpg", FileType.ProfileBanner, "http://old-banner.webp",
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Success(
                new ImageUploadResult(newFileId, "key", newUrl, "image/webp", 2048)));

        var handler = BuildHandler();
        var command = new UploadProfileBannerCommand(userId, stream, "banner.jpg", stream.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileId.Should().Be(newFileId);
        result.Data.Url.Should().Be(newUrl);

        var updated = await DbContext.UserProfiles.FindAsync(profile.Id);
        updated.Should().NotBeNull();
        updated!.CoverImageUrl.Should().Be(newUrl);
    }
}
