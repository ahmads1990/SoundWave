using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Artists.RejectArtistAccount;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Artists;

public class RejectArtistAccountTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<RejectArtistAccountCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly RejectArtistAccountRequestValidator _validator = new();

    private RejectArtistAccountCommandHandler BuildHandler()
    {
        return new RejectArtistAccountCommandHandler(
            CreateRepository<ArtistAccountApproval>(),
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

        var command = new RejectArtistAccountCommand(Guid.NewGuid(), "Incomplete info");
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

        var command = new RejectArtistAccountCommand(Guid.NewGuid(), "Reason");
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
            StageName = "Already Rejected",
            Status = ArtistApprovalStatus.Rejected,
            RejectionReason = "Prior rejection"
        };
        await SeedAsync(approval);

        var command = new RejectArtistAccountCommand(approval.Id, "Second rejection");
        var handler = BuildHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistApplicationAlreadyProcessed);
    }

    [Fact]
    public async Task Handle_ShouldRejectApplication_WhenValid()
    {
        var adminId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(adminId);

        var approval = new ArtistAccountApproval
        {
            UserId = applicantId,
            StageName = "Sample Artist",
            Status = ArtistApprovalStatus.Pending
        };
        await SeedAsync(approval);

        var command = new RejectArtistAccountCommand(approval.Id, "Invalid social links provided");
        var handler = BuildHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(approval.Id);

        var updatedApproval = await DbContext.ArtistAccountApprovals.FirstOrDefaultAsync(a => a.Id == approval.Id);
        updatedApproval.Should().NotBeNull();
        updatedApproval!.Status.Should().Be(ArtistApprovalStatus.Rejected);
        updatedApproval.RejectionReason.Should().Be("Invalid social links provided");
        updatedApproval.ReviewedBy.Should().Be(adminId);

        _publishEndpointMock.Verify(x => x.Publish(
            It.Is<ArtistApplicationRejectedEvent>(e => e.ApplicationId == approval.Id && e.UserId == applicantId && e.RejectionReason == "Invalid social links provided"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenReasonIsEmpty()
    {
        var request = new RejectArtistAccountRequest("");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenReasonExceeds500Characters()
    {
        var longReason = new string('R', 501);
        var request = new RejectArtistAccountRequest(longReason);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage.Contains("exceed 500"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenReasonIsValid()
    {
        var request = new RejectArtistAccountRequest("Duplicate request");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
