using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Artists.ApproveArtistAccount;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Artists;

public class ApproveArtistAccountTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<ApproveArtistAccountCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly ApproveArtistAccountRequestValidator _validator = new();

    private ApproveArtistAccountCommandHandler BuildHandler()
    {
        return new ApproveArtistAccountCommandHandler(
            CreateRepository<ArtistAccountApproval>(),
            CreateRepository<Artist>(),
            _currentUserServiceMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnUserNotAuthenticated_WhenAdminNotAuthenticated()
    {
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);

        var command = new ApproveArtistAccountCommand(Guid.NewGuid());
        var handler = BuildHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UserNotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnArtistApplicationNotFound_WhenIdDoesNotExist()
    {
        var adminId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

        var command = new ApproveArtistAccountCommand(Guid.NewGuid());
        var handler = BuildHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistApplicationNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnArtistApplicationAlreadyProcessed_WhenStatusIsNotPending()
    {
        var adminId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

        var approval = new ArtistAccountApproval
        {
            UserId = Guid.NewGuid(),
            StageName = "Already Approved",
            Status = ArtistApprovalStatus.Approved
        };
        await SeedAsync(approval);

        var command = new ApproveArtistAccountCommand(approval.Id);
        var handler = BuildHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistApplicationAlreadyProcessed);
    }

    [Fact]
    public async Task Handle_ShouldApproveApplicationAndCreateArtist_WhenValid()
    {
        var adminId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

        var approval = new ArtistAccountApproval
        {
            UserId = applicantId,
            StageName = "The Rockstars",
            Bio = "Great indie band",
            Status = ArtistApprovalStatus.Pending
        };
        await SeedAsync(approval);

        var command = new ApproveArtistAccountCommand(approval.Id);
        var handler = BuildHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var updatedApproval = await DbContext.ArtistAccountApprovals.FirstOrDefaultAsync(a => a.Id == approval.Id);
        updatedApproval.Should().NotBeNull();
        updatedApproval!.Status.Should().Be(ArtistApprovalStatus.Approved);
        updatedApproval.ReviewedBy.Should().Be(adminId);

        var createdArtist = await DbContext.Artists.FirstOrDefaultAsync(a => a.Id == result.Data);
        createdArtist.Should().NotBeNull();
        createdArtist!.UserId.Should().Be(applicantId);
        createdArtist.StageName.Should().Be("The Rockstars");
        createdArtist.Bio.Should().Be("Great indie band");

        _publishEndpointMock.Verify(x => x.Publish(
            It.Is<ArtistApplicationApprovedEvent>(e => e.ApplicationId == approval.Id && e.UserId == applicantId && e.ArtistId == result.Data),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenApplicationIdIsEmpty()
    {
        var request = new ApproveArtistAccountRequest(Guid.Empty);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenApplicationIdIsValid()
    {
        var request = new ApproveArtistAccountRequest(Guid.NewGuid());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
