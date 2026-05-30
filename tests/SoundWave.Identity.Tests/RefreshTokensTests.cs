using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Features.RefreshTokens;
using SoundWave.Identity.Helpers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

/// <summary>
/// Contains unit tests for the <see cref="RefreshTokensCommandHandler"/> class.
/// </summary>
public class RefreshTokensTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenHelper> _tokenHelperMock = new();
    private readonly Mock<ILogger<RefreshTokensCommandHandler>> _loggerMock = new();
    private readonly RefreshTokensCommandHandler _handler;

    public RefreshTokensTests()
    {
        _handler = new RefreshTokensCommandHandler(
            _refreshTokenRepoMock.Object,
            _userRepositoryMock.Object,
            _tokenHelperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenRefreshTokenIsInvalid()
    {
        // TODO: Implement unit test for invalid refresh token scenario
        await Task.CompletedTask;
    }
}
