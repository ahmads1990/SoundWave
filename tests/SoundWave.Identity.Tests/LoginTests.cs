using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Features.Login;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Models.Responses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

/// <summary>
/// Contains unit tests for the <see cref="LoginCommandHandler"/> class.
/// All database calls are mocked rather than using an in-memory database.
/// </summary>
public class LoginTests
{
    #region Fields

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenHelper> _tokenHelperMock = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();
    private readonly IOptions<JwtConfig> _jwtOptions;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginTests"/> class.
    /// Sets up the necessary configurations and JWT options.
    /// </summary>
    public LoginTests()
    {
        var jwtConfig = new JwtConfig
        {
            Key = "SuperSecretAndSecureKeyForTestingJWTTokensSigning",
            Issuer = "SoundWaveTestIssuer",
            Audience = "SoundWaveTestAudience",
            DurationInHours = 1,
            RefreshTokenLifeInDays = 30
        };
        _jwtOptions = Options.Create(jwtConfig);
    }

    #endregion

    #region Unit Tests

    /// <summary>
    /// Verifies that login fails with InvalidCredentials when the user email does not exist in the database.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnInvalidCredentials_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetUserLoginInfoByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserLoginInfoDto?)null);

        var handler = new LoginCommandHandler(_userRepositoryMock.Object, _tokenHelperMock.Object, _jwtOptions, _loggerMock.Object);
        var command = new LoginCommand("nonexistent@example.com", "Password123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ApiErrorCode.InvalidCredentials);
    }

    /// <summary>
    /// Verifies that login fails with InvalidCredentials when the user password verification fails.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnInvalidCredentials_WhenPasswordIsIncorrect()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var loginInfo = new UserLoginInfoDto
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = passwordHash,
            IsEmailVerified = true
        };

        _userRepositoryMock
            .Setup(r => r.GetUserLoginInfoByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginInfo);

        var handler = new LoginCommandHandler(_userRepositoryMock.Object, _tokenHelperMock.Object, _jwtOptions, _loggerMock.Object);
        var command = new LoginCommand("user@example.com", "WrongPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ApiErrorCode.InvalidCredentials);
    }

    /// <summary>
    /// Verifies that login fails with EmailNotVerified and includes the UserId in the payload when the email has not been verified yet.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnEmailNotVerified_WhenEmailIsNotVerified()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var userId = Guid.NewGuid();
        var loginInfo = new UserLoginInfoDto
        {
            Id = userId,
            Email = "unverified@example.com",
            PasswordHash = passwordHash,
            IsEmailVerified = false
        };

        _userRepositoryMock
            .Setup(r => r.GetUserLoginInfoByEmailAsync("unverified@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginInfo);

        var handler = new LoginCommandHandler(_userRepositoryMock.Object, _tokenHelperMock.Object, _jwtOptions, _loggerMock.Object);
        var command = new LoginCommand("unverified@example.com", "CorrectPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ApiErrorCode.EmailNotVerified);
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
    }

    /// <summary>
    /// Verifies that login fails with InternalServerError when the token helper fails to generate a JWT.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnInternalServerError_WhenJwtGenerationFails()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var userId = Guid.NewGuid();
        var loginInfo = new UserLoginInfoDto
        {
            Id = userId,
            Email = "verified@example.com",
            PasswordHash = passwordHash,
            IsEmailVerified = true,
            Name = "John Doe",
            Username = "johndoe"
        };

        _userRepositoryMock
            .Setup(r => r.GetUserLoginInfoByEmailAsync("verified@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginInfo);

        _tokenHelperMock
            .Setup(t => t.GenerateJWT(It.IsAny<UserTokenBaseClaims>(), It.IsAny<List<UserClaim>>(), It.IsAny<int>()))
            .Returns(string.Empty); // Simulates failure by returning empty token

        var handler = new LoginCommandHandler(_userRepositoryMock.Object, _tokenHelperMock.Object, _jwtOptions, _loggerMock.Object);
        var command = new LoginCommand("verified@example.com", "CorrectPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ApiErrorCode.InternalServerError);
    }

    /// <summary>
    /// Verifies that login succeeds and returns valid JWT and refresh tokens when credentials are valid.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var userId = Guid.NewGuid();
        var loginInfo = new UserLoginInfoDto
        {
            Id = userId,
            Email = "verified@example.com",
            PasswordHash = passwordHash,
            IsEmailVerified = true,
            Name = "Jane Doe",
            Username = "janedoe"
        };

        _userRepositoryMock
            .Setup(r => r.GetUserLoginInfoByEmailAsync("verified@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginInfo);

        var expectedJwt = "mocked.jwt.token";
        var expectedRefreshToken = "mocked_refresh_token";

        _tokenHelperMock
            .Setup(t => t.GenerateJWT(
                It.Is<UserTokenBaseClaims>(c => c.Uid == userId && c.Email == "verified@example.com" && c.Name == "Jane Doe"),
                It.Is<List<UserClaim>>(list => list.Exists(c => c.Type == CustomClaimTypes.Username && c.Value == "janedoe")),
                0))
            .Returns(expectedJwt);

        _tokenHelperMock
            .Setup(t => t.GenerateAndSaveRefreshTokenAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRefreshToken);

        var handler = new LoginCommandHandler(_userRepositoryMock.Object, _tokenHelperMock.Object, _jwtOptions, _loggerMock.Object);
        var command = new LoginCommand("verified@example.com", "CorrectPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.JwtToken.Should().Be(expectedJwt);
        result.Data!.RefreshToken.Should().Be(expectedRefreshToken);
    }

    /// <summary>
    /// Verifies that login succeeds and uses empty values for Name and Username when the user profile is null or has empty values.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldUseEmptyProfileValues_WhenUserProfileIsEmpty()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var userId = Guid.NewGuid();
        var loginInfo = new UserLoginInfoDto
        {
            Id = userId,
            Email = "noprofile@example.com",
            PasswordHash = passwordHash,
            IsEmailVerified = true,
            Name = string.Empty,
            Username = string.Empty
        };

        _userRepositoryMock
            .Setup(r => r.GetUserLoginInfoByEmailAsync("noprofile@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginInfo);

        var expectedJwt = "mocked.jwt.token";
        var expectedRefreshToken = "mocked_refresh_token";

        _tokenHelperMock
            .Setup(t => t.GenerateJWT(
                It.Is<UserTokenBaseClaims>(c => c.Uid == userId && c.Email == "noprofile@example.com" && c.Name == string.Empty),
                It.Is<List<UserClaim>>(list => list.Exists(c => c.Type == CustomClaimTypes.Username && c.Value == string.Empty)),
                0))
            .Returns(expectedJwt);

        _tokenHelperMock
            .Setup(t => t.GenerateAndSaveRefreshTokenAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRefreshToken);

        var handler = new LoginCommandHandler(_userRepositoryMock.Object, _tokenHelperMock.Object, _jwtOptions, _loggerMock.Object);
        var command = new LoginCommand("noprofile@example.com", "CorrectPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.JwtToken.Should().Be(expectedJwt);
        result.Data!.RefreshToken.Should().Be(expectedRefreshToken);
    }

    #endregion
}
