using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.PasswordReset;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Tests;

public class ResetPasswordTests : IdentityIntegrationTestBase
{
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ILogger<ResetPasswordCommandHandler>> _loggerMock = new();

    private ResetPasswordCommandHandler BuildHandler()
    {
        var userRepository = new IdentityRepository<User>(DbContext);
        return new ResetPasswordCommandHandler(
            userRepository,
            _cachingServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        var handler = BuildHandler();
        var command = new ResetPasswordCommand("nonexistent@example.com", "123456", "NewPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidToken_WhenTokenIsMissingFromCache()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsEmailVerified = true,
            Role = UserRole.Listener
        });
        
        var expectedCacheKey = Constants.Caching.GetUserPasswordResetKey(userId);
        _cachingServiceMock.Setup(c => c.GetAsync(expectedCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        var handler = BuildHandler();
        var command = new ResetPasswordCommand("user@example.com", "123456", "NewPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidToken_WhenTokenDoesNotMatch()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsEmailVerified = true,
            Role = UserRole.Listener
        });
        
        var expectedCacheKey = Constants.Caching.GetUserPasswordResetKey(userId);
        _cachingServiceMock.Setup(c => c.GetAsync(expectedCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("654321"); // Stored token is different

        var handler = BuildHandler();
        var command = new ResetPasswordCommand("user@example.com", "123456", "NewPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePasswordAndClearLockout_WhenTokenIsValid()
    {
        var userId = Guid.CreateVersion7();
        var originalHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!");
        await SeedAsync(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = originalHash,
            IsEmailVerified = true,
            Role = UserRole.Listener,
            LockoutUntilUtc = DateTime.UtcNow.AddYears(100) // Hard locked
        });
        
        var expectedCacheKey = Constants.Caching.GetUserPasswordResetKey(userId);
        _cachingServiceMock.Setup(c => c.GetAsync(expectedCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        var handler = BuildHandler();
        var command = new ResetPasswordCommand("user@example.com", "123456", "NewPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        
        // Fetch user from DB
        var updatedUser = await DbContext.Users.FirstAsync(u => u.Id == userId);

        // Ensure new hash is generated and different from old hash
        updatedUser.PasswordHash.Should().NotBe(originalHash);
        BCrypt.Net.BCrypt.Verify("NewPassword123!", updatedUser.PasswordHash).Should().BeTrue();
        
        // Ensure lockout is cleared
        updatedUser.LockoutUntilUtc.Should().BeNull();
        
        // Verify caches cleared
        _cachingServiceMock.Verify(c => c.RemoveAsync(expectedCacheKey, It.IsAny<CancellationToken>()), Times.Once);
        
        var softLockKey = Constants.Caching.GetUserFailedLoginKey(userId);
        var hardLockKey = Constants.Caching.GetUserHardFailedLoginKey(userId);
        _cachingServiceMock.Verify(c => c.RemoveAsync(softLockKey, It.IsAny<CancellationToken>()), Times.Once);
        _cachingServiceMock.Verify(c => c.RemoveAsync(hardLockKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
