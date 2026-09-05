using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Messaging.Consumers;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Tests.Messaging;

public class ArtistApplicationConsumerTests : IdentityIntegrationTestBase
{
    private readonly Mock<ILogger<ArtistApplicationApprovedConsumer>> _approvedLoggerMock = new();

    #region Approved Consumer Tests

    [Fact]
    public async Task ApprovedConsumer_ShouldUpgradeRole_WhenUserExists()
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

        var consumer = new ArtistApplicationApprovedConsumer(DbContext, _approvedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationApprovedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationApprovedEvent(Guid.NewGuid(), user.Id, Guid.NewGuid()));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var updatedUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Role.Should().Be(UserRole.Artist);
    }

    [Fact]
    public async Task ApprovedConsumer_ShouldReturnEarly_WhenUserDoesNotExist()
    {
        var consumer = new ArtistApplicationApprovedConsumer(DbContext, _approvedLoggerMock.Object);
        var contextMock = new Mock<ConsumeContext<ArtistApplicationApprovedEvent>>();
        contextMock.Setup(x => x.Message).Returns(new ArtistApplicationApprovedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var act = () => consumer.Consume(contextMock.Object);
        await act.Should().NotThrowAsync();
    }

    #endregion
}
