using Microsoft.EntityFrameworkCore;
using Moq;
using SoundWave.Identity.Data;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.Testing.Core;

namespace SoundWave.Identity.Tests;

public abstract class IdentityIntegrationTestBase : IntegrationTestBase
{
    internal IdentityDbContext DbContext => (IdentityDbContext)BaseDbContext;
    protected override DbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);

        return new IdentityDbContext(options, currentUserServiceMock.Object);
    }
}
