using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.ApproveArtistAccount;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Catalog.Tests.Features;

public class ApproveArtistAccountTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<ApproveArtistAccountCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IOutboxService> _outboxServiceMock = new();
    private readonly ApproveArtistAccountRequestValidator _validator = new();

    private ApproveArtistAccountCommandHandler BuildHandler()
    {
        return new ApproveArtistAccountCommandHandler(DbContext, _currentUserServiceMock.Object, _outboxServiceMock.Object, _loggerMock.Object);
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

        _outboxServiceMock.Verify(x => x.WriteOutboxMessage(
            It.Is<OutboxMessageRequest>(r => r.RoutingKey == Constants.MessageBus.RoutingKeys.ArtistApplicationApproved),
            DbContext), Times.Once);
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
