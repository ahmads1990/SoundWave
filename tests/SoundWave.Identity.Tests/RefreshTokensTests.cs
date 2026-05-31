using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Features.RefreshTokens;
using SoundWave.Identity.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

public class RefreshTokensTests : IdentityIntegrationTestBase
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ILogger<RefreshTokensCommandHandler>> _loggerMock = new();

    private RefreshTokensCommandHandler BuildHandler()
    {
        var refreshTokenRepo = new IdentityRepository<RefreshToken>(DbContext);
        var userRepository = new IdentityRepository<User>(DbContext);
        return new RefreshTokensCommandHandler(
            refreshTokenRepo,
            userRepository,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenRefreshTokenIsInvalid()
    {
        var handler = BuildHandler();
        var command = new RefreshTokensCommand(Guid.CreateVersion7(), "invalid_token");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnTokens_WhenRefreshTokenIsValid()
    {
        var userId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();
        
        await SeedAsync(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            IsLocked = false,
            Role = UserRole.Listener
        });

        var tokenHash = BCrypt.Net.BCrypt.HashPassword("valid_token");
        await SeedAsync(new RefreshToken
        {
            Id = tokenId,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedDate = DateTime.UtcNow
        });

        _tokenServiceMock
            .Setup(t => t.VerifyToken("valid_token", tokenHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(t => t.GenerateUserTokensAsync(It.IsAny<UserLoginInfoDto>(), tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserTokensDto { JwtToken = "new_jwt", RefreshToken = "new_refresh_token" });

        var handler = BuildHandler();
        var command = new RefreshTokensCommand(userId, "valid_token");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.JwtToken.Should().Be("new_jwt");
        result.Data!.RefreshToken.Should().Be("new_refresh_token");
    }
}
