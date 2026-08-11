using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.GetMyArtistApplicationStatus;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features;

public class GetMyArtistApplicationStatusTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<GetMyArtistApplicationStatusQueryHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private GetMyArtistApplicationStatusQueryHandler BuildHandler()
    {
        return new GetMyArtistApplicationStatusQueryHandler(
            CreateReadDbContext(),
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnUserNotAuthenticated_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);

        var query = new GetMyArtistApplicationStatusQuery();
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UserNotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnArtistApplicationNotFound_WhenNoApplicationExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var query = new GetMyArtistApplicationStatusQuery();
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistApplicationNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnApplicationDetails_WhenApplicationExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var approval = new ArtistAccountApproval
        {
            UserId = userId,
            StageName = "Luna Sound",
            Bio = "Synthwave artist",
            Status = ArtistApprovalStatus.Rejected,
            RejectionReason = "Please provide social links",
            ReviewedAt = DateTime.UtcNow
        };

        await SeedAsync(approval);

        var query = new GetMyArtistApplicationStatusQuery();
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.StageName.Should().Be("Luna Sound");
        result.Data.Bio.Should().Be("Synthwave artist");
        result.Data.Status.Should().Be(ArtistApprovalStatus.Rejected);
        result.Data.RejectionReason.Should().Be("Please provide social links");
    }

    #endregion
}
