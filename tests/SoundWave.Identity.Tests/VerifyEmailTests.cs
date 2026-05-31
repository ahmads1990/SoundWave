using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.VerifyEmail;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Tests;

public class VerifyEmailTests : IdentityIntegrationTestBase
{
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ILogger<VerifyEmailCommandHandler>> _loggerMock = new();

    private VerifyEmailCommandHandler BuildHandler()
    {
        var userRepository = new IdentityRepository<User>(DbContext);
        return new VerifyEmailCommandHandler(
            userRepository,
            _cachingServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserNotFound_WhenEmailDoesNotExist()
    {
        var handler = BuildHandler();
        var command = new VerifyEmailCommand("nonexistent@example.com", "123456");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmailAlreadyVerified_WhenUserIsAlreadyVerified()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "verified@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = UserRole.Listener
        });

        var handler = BuildHandler();
        var command = new VerifyEmailCommand("verified@example.com", "123456");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailAlreadyVerified);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidToken_WhenOtpIsMissingFromCache()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            IsEmailVerified = false,
            Role = UserRole.Listener
        });

        var cacheKey = Constants.Caching.UserEmailVerification + userId.ToString();
        _cachingServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var handler = BuildHandler();
        var command = new VerifyEmailCommand("test@example.com", "123456");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidToken_WhenOtpDoesNotMatch()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            IsEmailVerified = false,
            Role = UserRole.Listener
        });

        var cacheKey = Constants.Caching.UserEmailVerification + userId.ToString();
        _cachingServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("wrongOTP");

        var handler = BuildHandler();
        var command = new VerifyEmailCommand("test@example.com", "123456");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndUpdateUser_WhenOtpIsValid()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            IsEmailVerified = false,
            Role = UserRole.Listener
        });

        var cacheKey = Constants.Caching.UserEmailVerification + userId.ToString();
        _cachingServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        var handler = BuildHandler();
        var command = new VerifyEmailCommand("test@example.com", "123456");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        updatedUser!.IsEmailVerified.Should().BeTrue();

        _cachingServiceMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
