using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Features.Login;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

public class LoginTests : IdentityIntegrationTestBase
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();

    private LoginCommandHandler BuildHandler()
    {
        var userRepository = new IdentityRepository<User>(DbContext);
        return new LoginCommandHandler(userRepository, _tokenServiceMock.Object, _cachingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidCredentials_WhenUserDoesNotExist()
    {
        var handler = BuildHandler();
        var command = new LoginCommand("nonexistent@example.com", "Password123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidCredentials_WhenPasswordIsIncorrect()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsEmailVerified = true,
            Role = UserRole.Listener
        });

        _cachingServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("0");

        var handler = BuildHandler();
        var command = new LoginCommand("user@example.com", "WrongPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmailNotVerified_WhenEmailIsNotVerified()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "unverified@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsEmailVerified = false,
            Role = UserRole.Listener
        });

        var handler = BuildHandler();
        var command = new LoginCommand("unverified@example.com", "CorrectPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailNotVerified);
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
    }


    [Fact]
    public async Task Handle_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "verified@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsEmailVerified = true,
            Role = UserRole.Listener,
            UserProfile = new UserProfile
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                FirstName = "Jane",
                LastName = "Doe",
                DisplayName = "janedoe"
            }
        });

        var expectedJwt = "mocked.jwt.token";
        var expectedRefreshToken = "mocked_refresh_token";

        _tokenServiceMock
            .Setup(t => t.GenerateUserTokensAsync(It.IsAny<UserLoginInfoDto>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserTokensDto { JwtToken = expectedJwt, RefreshToken = expectedRefreshToken });

        var handler = BuildHandler();
        var command = new LoginCommand("verified@example.com", "CorrectPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.JwtToken.Should().Be(expectedJwt);
        result.Data!.RefreshToken.Should().Be(expectedRefreshToken);
    }

    [Fact]
    public async Task Handle_ShouldLockAccount_WhenMaxFailedAttemptsReached()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsEmailVerified = true,
            IsLocked = false,
            Role = UserRole.Listener
        });

        _cachingServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Constants.MAX_FAILED_LOGIN_ATTEMPTS - 1).ToString());

        var handler = BuildHandler();
        var command = new LoginCommand("user@example.com", "WrongPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.AccountLocked);

        var lockedUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        lockedUser!.IsLocked.Should().BeTrue();
        
        _cachingServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
