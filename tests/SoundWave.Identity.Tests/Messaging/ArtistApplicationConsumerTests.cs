using FluentAssertions;
using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Messaging.Consumers;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Identity.Tests.Messaging;

public class ArtistApplicationConsumerTests : IdentityIntegrationTestBase
{
    private readonly Mock<IBackgroundJobClient> _jobClientMock = new();
    private readonly Mock<ILogger<ArtistApplicationApprovedConsumer>> _approvedLoggerMock = new();
    private readonly Mock<ILogger<ArtistApplicationSubmittedConsumer>> _submittedLoggerMock = new();
    private readonly Mock<ILogger<ArtistApplicationRejectedConsumer>> _rejectedLoggerMock = new();

    #region Approved Consumer Tests

    [Fact]
    public async Task ApprovedConsumer_ShouldUpgradeRoleAndEnqueueEmail_WhenUserExists()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "artist@soundwave.com",
            PasswordHash = "hash",
            Role = UserRole.Listener,
            UserProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FirstName = "David",
                LastName = "Bowie"
            }
        };
        await SeedAsync(user);

        var consumer = new ArtistApplicationApprovedConsumer(DbContext, _jobClientMock.Object, _approvedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationApprovedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationApprovedEvent(Guid.NewGuid(), user.Id, Guid.NewGuid()));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var updatedUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Role.Should().Be(UserRole.Artist);

        _jobClientMock.Verify(x => x.Create(
            It.Is<Hangfire.Common.Job>(j => j.Method.Name == "Execute"),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task ApprovedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationApprovedConsumer(DbContext, _jobClientMock.Object, _approvedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationApprovedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationApprovedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Never);
    }

    #endregion

    #region Submitted Consumer Tests

    [Fact]
    public async Task SubmittedConsumer_ShouldEnqueueEmail_WhenUserExists()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "applicant@soundwave.com",
            PasswordHash = "hash",
            Role = UserRole.Listener,
            UserProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FirstName = "Prince",
                LastName = "Nelson"
            }
        };
        await SeedAsync(user);

        var consumer = new ArtistApplicationSubmittedConsumer(DbContext, _jobClientMock.Object, _submittedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationSubmittedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationSubmittedEvent(Guid.NewGuid(), user.Id, "Prince"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.Is<Hangfire.Common.Job>(j => j.Method.Name == "Execute"),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task SubmittedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationSubmittedConsumer(DbContext, _jobClientMock.Object, _submittedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationSubmittedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationSubmittedEvent(Guid.NewGuid(), Guid.NewGuid(), "Unknown"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Never);
    }

    #endregion

    #region Rejected Consumer Tests

    [Fact]
    public async Task RejectedConsumer_ShouldEnqueueEmailWithReason_WhenUserExists()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "rejected@soundwave.com",
            PasswordHash = "hash",
            Role = UserRole.Listener,
            UserProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FirstName = "Freddie",
                LastName = "Mercury"
            }
        };
        await SeedAsync(user);

        var consumer = new ArtistApplicationRejectedConsumer(DbContext, _jobClientMock.Object, _rejectedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationRejectedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationRejectedEvent(Guid.NewGuid(), user.Id, "Incomplete documentation"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.Is<Hangfire.Common.Job>(j => j.Method.Name == "Execute"),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task RejectedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationRejectedConsumer(DbContext, _jobClientMock.Object, _rejectedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationRejectedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationRejectedEvent(Guid.NewGuid(), Guid.NewGuid(), "Some reason"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Never);
    }

    #endregion
}
