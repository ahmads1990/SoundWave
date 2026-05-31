using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Features.Logout;
using SoundWave.Identity.Services;

namespace SoundWave.Identity.Tests;

public class LogoutTests : IdentityIntegrationTestBase
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ILogger<LogoutCommandHandler>> _loggerMock = new();

    private LogoutCommandHandler BuildHandler()
    {
        return new LogoutCommandHandler(_tokenServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIdIsEmpty()
    {
        var handler = BuildHandler();
        var command = new LogoutCommand(Guid.Empty, "jti_value", DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenJtiIsEmpty()
    {
        var handler = BuildHandler();
        var command = new LogoutCommand(Guid.CreateVersion7(), "", DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.InvalidToken);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAndBlacklist_WhenCommandIsValid()
    {
        var userId = Guid.CreateVersion7();
        var jti = "valid_jti";
        var expiryDate = DateTime.UtcNow.AddMinutes(15);

        _tokenServiceMock
            .Setup(t => t.RevokeActiveRefreshToken(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tokenServiceMock
            .Setup(t => t.BlacklistJtiAsync(jti, expiryDate, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = BuildHandler();
        var command = new LogoutCommand(userId, jti, expiryDate);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        _tokenServiceMock.Verify(t => t.RevokeActiveRefreshToken(userId, It.IsAny<CancellationToken>()), Times.Once);
        _tokenServiceMock.Verify(t => t.BlacklistJtiAsync(jti, expiryDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRevokeButNotBlacklist_WhenExpiryDateIsNull()
    {
        var userId = Guid.CreateVersion7();
        var jti = "valid_jti";

        _tokenServiceMock
            .Setup(t => t.RevokeActiveRefreshToken(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = BuildHandler();
        var command = new LogoutCommand(userId, jti, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        _tokenServiceMock.Verify(t => t.RevokeActiveRefreshToken(userId, It.IsAny<CancellationToken>()), Times.Once);
        _tokenServiceMock.Verify(t => t.BlacklistJtiAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
