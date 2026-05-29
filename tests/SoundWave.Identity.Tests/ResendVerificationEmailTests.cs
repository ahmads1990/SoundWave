using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Events.Notifications.VerificationEmailRequested;
using SoundWave.Identity.Features.ResendVerificationEmail;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ResendVerificationEmailCommandHandler"/> class.
/// </summary>
public class ResendVerificationEmailTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ITokenHelper> _tokenHelperMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<ResendVerificationEmailCommandHandler>> _loggerMock = new();
    private readonly ResendVerificationEmailCommandHandler _handler;

    public ResendVerificationEmailTests()
    {
        _handler = new ResendVerificationEmailCommandHandler(
            _userRepositoryMock.Object,
            _cachingServiceMock.Object,
            _tokenHelperMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserNotFound_WhenEmailDoesNotExist()
    {
        // Arrange
        var command = new ResendVerificationEmailCommand("nonexistent@example.com");

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVerificationInfoDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
        _publisherMock.Verify(p => p.Publish(It.IsAny<VerificationEmailRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmailAlreadyVerified_WhenUserIsAlreadyVerified()
    {
        // Arrange
        var command = new ResendVerificationEmailCommand("verified@example.com");
        var userInfo = new UserVerificationInfoDto { Id = Guid.NewGuid(), Email = command.Email, IsEmailVerified = true };

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailAlreadyVerified);
        _publisherMock.Verify(p => p.Publish(It.IsAny<VerificationEmailRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndPublishEvent_WhenUserExistsAndNotVerified()
    {
        // Arrange
        var command = new ResendVerificationEmailCommand("unverified@example.com");
        var user = new User { Id = Guid.NewGuid(), Email = command.Email, IsEmailVerified = false };
        var profile = new UserProfile { UserId = user.Id, FirstName = "Test", LastName = "User" };

        var userInfo = new UserVerificationInfoDto
        {
            Id = user.Id,
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            FirstName = profile.FirstName,
            LastName = profile.LastName
        };

        _userRepositoryMock
            .Setup(r => r.GetUserVerificationInfoByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _tokenHelperMock
            .Setup(t => t.GenerateOTP(It.IsAny<int>()))
            .Returns("123456");

        _cachingServiceMock
            .Setup(c => c.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _cachingServiceMock.Verify(
            c => c.AddAsync(
                Constants.Caching.UserEmailVerification + user.Id.ToString(),
                "123456",
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _publisherMock.Verify(
            p => p.Publish(
                It.Is<VerificationEmailRequestedNotification>(n => 
                    n.UserId == user.Id && 
                    n.Email == user.Email && 
                    n.FullName == "Test User" && 
                    n.Otp == "123456"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
