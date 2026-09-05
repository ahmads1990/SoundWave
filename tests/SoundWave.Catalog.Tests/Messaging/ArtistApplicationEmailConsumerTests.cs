using FluentAssertions;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Messaging.Consumers;

namespace SoundWave.Catalog.Tests.Messaging;

public class ArtistApplicationEmailConsumerTests : CatalogIntegrationTestBase
{
    private readonly Mock<IBackgroundJobClient> _jobClientMock = new();
    private readonly Mock<ILogger<ArtistApplicationSubmittedEmailConsumer>> _submittedLoggerMock = new();
    private readonly Mock<ILogger<ArtistApplicationApprovedEmailConsumer>> _approvedLoggerMock = new();
    private readonly Mock<ILogger<ArtistApplicationRejectedEmailConsumer>> _rejectedLoggerMock = new();

    #region Submitted Consumer Tests

    [Fact]
    public async Task SubmittedConsumer_ShouldEnqueueEmail_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        await SeedUserLookupAsync(userId, "applicant@soundwave.com", "Prince", "Nelson");

        var consumer = new ArtistApplicationSubmittedEmailConsumer(
            CreateReadDbContext(),
            _jobClientMock.Object,
            _submittedLoggerMock.Object);

        var contextMock = new Mock<ConsumeContext<ArtistApplicationSubmittedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationSubmittedEvent(Guid.NewGuid(), userId, "Prince"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.Is<Hangfire.Common.Job>(j => j.Method.Name == "Execute"),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task SubmittedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationSubmittedEmailConsumer(
            CreateReadDbContext(),
            _jobClientMock.Object,
            _submittedLoggerMock.Object);

        var contextMock = new Mock<ConsumeContext<ArtistApplicationSubmittedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationSubmittedEvent(Guid.NewGuid(), Guid.NewGuid(), "Unknown"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Never);
    }

    #endregion

    #region Approved Consumer Tests

    [Fact]
    public async Task ApprovedConsumer_ShouldEnqueueEmail_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        await SeedUserLookupAsync(userId, "artist@soundwave.com", "David", "Bowie");

        var consumer = new ArtistApplicationApprovedEmailConsumer(
            CreateReadDbContext(),
            _jobClientMock.Object,
            _approvedLoggerMock.Object);

        var contextMock = new Mock<ConsumeContext<ArtistApplicationApprovedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationApprovedEvent(Guid.NewGuid(), userId, Guid.NewGuid()));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.Is<Hangfire.Common.Job>(j => j.Method.Name == "Execute"),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task ApprovedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationApprovedEmailConsumer(
            CreateReadDbContext(),
            _jobClientMock.Object,
            _approvedLoggerMock.Object);

        var contextMock = new Mock<ConsumeContext<ArtistApplicationApprovedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationApprovedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
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
        var userId = Guid.NewGuid();
        await SeedUserLookupAsync(userId, "rejected@soundwave.com", "Freddie", "Mercury");

        var consumer = new ArtistApplicationRejectedEmailConsumer(
            CreateReadDbContext(),
            _jobClientMock.Object,
            _rejectedLoggerMock.Object);

        var contextMock = new Mock<ConsumeContext<ArtistApplicationRejectedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationRejectedEvent(Guid.NewGuid(), userId, "Incomplete documentation"));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        _jobClientMock.Verify(x => x.Create(
            It.Is<Hangfire.Common.Job>(j => j.Method.Name == "Execute"),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task RejectedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationRejectedEmailConsumer(
            CreateReadDbContext(),
            _jobClientMock.Object,
            _rejectedLoggerMock.Object);

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
