using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Features.VerifyEmail;
using SoundWave.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

/// <summary>
/// Contains unit tests for the <see cref="VerifyEmailCommandHandler"/> class.
/// </summary>
public class VerifyEmailTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ILogger<VerifyEmailCommandHandler>> _loggerMock = new();
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailTests()
    {
        _handler = new VerifyEmailCommandHandler(
            _userRepositoryMock.Object,
            _cachingServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserNotFound_WhenEmailDoesNotExist()
    {
        // Arrange
        var command = new VerifyEmailCommand("nonexistent@example.com", "123456");

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVerificationInfoDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
        _userRepositoryMock.Verify(r => r.SaveInclude(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmailAlreadyVerified_WhenUserIsAlreadyVerified()
    {
        // Arrange
        var command = new VerifyEmailCommand("verified@example.com", "123456");
        var userInfo = new UserVerificationInfoDto { Id = Guid.NewGuid(), Email = command.Email, IsEmailVerified = true };

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailAlreadyVerified);
        _userRepositoryMock.Verify(r => r.SaveInclude(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidToken_WhenOtpIsMissingFromCache()
    {
        // Arrange
        var command = new VerifyEmailCommand("test@example.com", "123456");
        var userInfo = new UserVerificationInfoDto { Id = Guid.NewGuid(), Email = command.Email, IsEmailVerified = false };
        var cacheKey = Constants.Caching.UserEmailVerification + userInfo.Id.ToString();

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _cachingServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
        _userRepositoryMock.Verify(r => r.SaveInclude(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidToken_WhenOtpDoesNotMatch()
    {
        // Arrange
        var command = new VerifyEmailCommand("test@example.com", "wrongOTP");
        var userInfo = new UserVerificationInfoDto { Id = Guid.NewGuid(), Email = command.Email, IsEmailVerified = false };
        var cacheKey = Constants.Caching.UserEmailVerification + userInfo.Id.ToString();

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _cachingServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
        _userRepositoryMock.Verify(r => r.SaveInclude(It.IsAny<User>(), It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndUpdateUser_WhenOtpIsValid()
    {
        // Arrange
        var command = new VerifyEmailCommand("test@example.com", "123456");
        var userInfo = new UserVerificationInfoDto { Id = Guid.NewGuid(), Email = command.Email, IsEmailVerified = false };
        var cacheKey = Constants.Caching.UserEmailVerification + userInfo.Id.ToString();

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _cachingServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(command.Otp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _userRepositoryMock.Verify(r => r.SaveInclude(It.Is<User>(u => u.Id == userInfo.Id && u.IsEmailVerified == true), It.IsAny<string[]>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
        _cachingServiceMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
