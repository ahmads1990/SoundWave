using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Artists.ApplyForArtistAccount;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Artists;

public class ApplyForArtistAccountTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<ApplyForArtistAccountCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly ApplyForArtistAccountRequestValidator _validator = new();

    private ApplyForArtistAccountCommandHandler BuildHandler()
    {
        return new ApplyForArtistAccountCommandHandler(
            CreateRepository<ArtistAccountApproval>(),
            _currentUserServiceMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnUserNotAuthenticated_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);

        var command = new ApplyForArtistAccountCommand("DJ SoundWave", "Cool bio");
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UserNotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnArtistApplicationAlreadyExists_WhenUserAlreadyApplied()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        await SeedAsync(new ArtistAccountApproval
        {
            UserId = userId,
            StageName = "First Stage Name",
            Status = ArtistApprovalStatus.Pending
        });

        var command = new ApplyForArtistAccountCommand("Second Stage Name", "Duplicate attempt");
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistApplicationAlreadyExists);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserAppliesFirstTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var command = new ApplyForArtistAccountCommand("Ahmad Music", "Passionate indie producer");
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var savedApproval = await DbContext.ArtistAccountApprovals.FirstOrDefaultAsync(a => a.Id == result.Data);
        savedApproval.Should().NotBeNull();
        savedApproval!.UserId.Should().Be(userId);
        savedApproval.StageName.Should().Be("Ahmad Music");
        savedApproval.Bio.Should().Be("Passionate indie producer");
        savedApproval.Status.Should().Be(ArtistApprovalStatus.Pending);

        _publishEndpointMock.Verify(x => x.Publish(
            It.Is<ArtistApplicationSubmittedEvent>(e => e.ApplicationId == result.Data && e.UserId == userId && e.StageName == "Ahmad Music"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenStageNameIsEmpty()
    {
        var request = new ApplyForArtistAccountRequest("", "Some bio");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StageName" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenStageNameExceeds100Characters()
    {
        var longStageName = new string('A', 101);
        var request = new ApplyForArtistAccountRequest(longStageName, "Some bio");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StageName" && e.ErrorMessage.Contains("exceed 100"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenBioExceeds1000Characters()
    {
        var longBio = new string('B', 1001);
        var request = new ApplyForArtistAccountRequest("Stage Name", longBio);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Bio" && e.ErrorMessage.Contains("exceed 1000"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new ApplyForArtistAccountRequest("Indie Artist", "Short bio");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
