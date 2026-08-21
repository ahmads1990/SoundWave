using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.Account.PasswordReset;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Common;
using MediatR;
using SoundWave.Identity.Events.Notifications.PasswordResetRequested;

namespace SoundWave.Identity.Tests.Features.Account;

public class ForgotPasswordTests : IdentityIntegrationTestBase
{
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _loggerMock = new();

    private ForgotPasswordCommandHandler BuildHandler()
    {
        var userRepository = new IdentityRepository<User>(DbContext);
        return new ForgotPasswordCommandHandler(
            userRepository,
            _otpServiceMock.Object,
            _cachingServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailDoesNotExist_ShouldReturnFailureAndNotGenerateOtp()
    {
        var handler = BuildHandler();
        var command = new ForgotPasswordCommand("nonexistent@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);

        _otpServiceMock.Verify(x => x.GenerateOtp(It.IsAny<int>()), Times.Never);
        _cachingServiceMock.Verify(x => x.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldGenerateAndStoreOtp_WhenUserExists()
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

        _otpServiceMock.Setup(x => x.GenerateOtp(It.IsAny<int>())).Returns("123456");

        var handler = BuildHandler();
        var command = new ForgotPasswordCommand("user@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        
        _otpServiceMock.Verify(x => x.GenerateOtp(6), Times.Once);
        
        var expectedCacheKey = Constants.Caching.GetUserPasswordResetKey(userId);
        _cachingServiceMock.Verify(x => x.AddAsync(
            expectedCacheKey, 
            "123456", 
            TimeSpan.FromMinutes(Constants.Caching.UserPasswordResetTtlMinutes), 
            It.IsAny<CancellationToken>()), Times.Once);

        _publisherMock.Verify(x => x.Publish(
            It.Is<PasswordResetRequestedNotification>(n => n.Email == "user@example.com" && n.Otp == "123456"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
