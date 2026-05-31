using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Configs;
using Xunit;

namespace SoundWave.Identity.Tests.Services;

public class TokenServiceTests
{
    private static readonly JwtConfig TestConfig = new()
    {
        Key                    = "test-secret-key-minimum-32-chars!!",
        Issuer                 = "soundwave-test",
        Audience               = "soundwave-test",
        DurationInHours        = 1,
        RefreshTokenLifeInDays = 7
    };

    private static TokenService BuildService(Mock<IIdentityRepository<RefreshToken>> repoMock = null)
    {
        repoMock ??= new Mock<IIdentityRepository<RefreshToken>>();
        var loggerMock = new Mock<ILogger<TokenService>>();
        return new TokenService(Options.Create(TestConfig), repoMock.Object, loggerMock.Object);
    }

    private static UserLoginInfoDto BuildUser() => new()
    {
        Id          = Guid.NewGuid(),
        Email       = "ahmad@test.com",
        Name        = "Ahmad",
        Username    = "ahmad.test",
        Role        = Common.UserRole.Listener,
        PasswordHash = "irrelevant-for-token-tests"
    };

    [Fact]
    public async Task GenerateUserTokensAsync_ReturnsValidTokens_AndCallsSave()
    {
        var user  = BuildUser();
        var repoMock = new Mock<IIdentityRepository<RefreshToken>>();
        var svc   = BuildService(repoMock);
        
        var tokens = await svc.GenerateUserTokensAsync(user, null, CancellationToken.None);
        
        tokens.Should().NotBeNull();
        tokens.JwtToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        repoMock.Verify(r => r.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateUserTokensAsync_ContainsCorrectClaims()
    {
        var user  = BuildUser();
        var svc   = BuildService();
        var tokens = await svc.GenerateUserTokensAsync(user, null, CancellationToken.None);

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(tokens.JwtToken);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    [Fact]
    public async Task VerifyToken_ValidHash_ReturnsTrue()
    {
        var svc = BuildService();
        var tokens = await svc.GenerateUserTokensAsync(BuildUser());
        
        // We can't easily extract the hash from the mock since we didn't capture it.
        // We'll generate a hash manually to test VerifyToken
        var rawToken = "my-secret-refresh-token";
        var hash = BCrypt.Net.BCrypt.HashPassword(rawToken);

        svc.VerifyToken(rawToken, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyToken_WrongRaw_ReturnsFalse()
    {
        var svc  = BuildService();
        var rawToken = "my-secret-refresh-token";
        var hash = BCrypt.Net.BCrypt.HashPassword(rawToken);

        svc.VerifyToken("wrong-token", hash).Should().BeFalse();
    }

    [Fact]
    public async Task ReadExpiredToken_ExpiredToken_ReturnsPrincipal()
    {
        var expiredConfig = new JwtConfig
        {
            Key                    = TestConfig.Key,
            Issuer                 = TestConfig.Issuer,
            Audience               = TestConfig.Audience,
            DurationInHours        = -1, // already expired
            RefreshTokenLifeInDays = 7
        };

        var svc       = new TokenService(Options.Create(expiredConfig), new Mock<IIdentityRepository<RefreshToken>>().Object, new Mock<ILogger<TokenService>>().Object);
        var user      = BuildUser();
        var tokens    = await svc.GenerateUserTokensAsync(user, null, CancellationToken.None);
        var principal = svc.ReadExpiredToken(tokens.JwtToken);

        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value
            .Should().Be(user.Id.ToString());
    }

    [Fact]
    public void ReadExpiredToken_GarbageToken_ReturnsNull()
    {
        var svc       = BuildService();
        var principal = svc.ReadExpiredToken("this.is.garbage");
        principal.Should().BeNull();
    }
}
