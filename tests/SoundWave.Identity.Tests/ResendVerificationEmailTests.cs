using SoundWave.SharedKernel.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Events.Notifications.VerificationEmailRequested;
using SoundWave.Identity.Features.ResendVerificationEmail;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

public class ResendVerificationEmailTests : IdentityIntegrationTestBase
{
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<ResendVerificationEmailCommandHandler>> _loggerMock = new();

    private ResendVerificationEmailCommandHandler BuildHandler()
    {
        var userRepository = new IdentityRepository<User>(DbContext);
        return new ResendVerificationEmailCommandHandler(
            userRepository,
            _cachingServiceMock.Object,
            _otpServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserNotFound_WhenEmailDoesNotExist()
    {
        var handler = BuildHandler();
        var command = new ResendVerificationEmailCommand("nonexistent@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
        _publisherMock.Verify(p => p.Publish(It.IsAny<VerificationEmailRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var command = new ResendVerificationEmailCommand("verified@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailAlreadyVerified);
        _publisherMock.Verify(p => p.Publish(It.IsAny<VerificationEmailRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceedAndPublishEvent_WhenUserExistsAndNotVerified()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "unverified@example.com",
            PasswordHash = "hash",
            IsEmailVerified = false,
            Role = UserRole.Listener,
            UserProfile = new UserProfile
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                FirstName = "Test",
                LastName = "User"
            }
        });

        _otpServiceMock
            .Setup(t => t.GenerateOtp(It.IsAny<int>()))
            .Returns("123456");

        var handler = BuildHandler();
        var command = new ResendVerificationEmailCommand("unverified@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _cachingServiceMock.Verify(
            c => c.AddAsync(
                Constants.Caching.UserEmailVerification + userId.ToString(),
                "123456",
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _publisherMock.Verify(
            p => p.Publish(
                It.Is<VerificationEmailRequestedNotification>(n => 
                    n.UserId == userId && 
                    n.Email == "unverified@example.com" && 
                    n.FullName == "Test User" && 
                    n.Otp == "123456"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
