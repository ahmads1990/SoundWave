using Microsoft.EntityFrameworkCore;
using Moq;
using SoundWave.Catalog.Data;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.Testing.Core;

namespace SoundWave.Catalog.Tests;

/// <summary>
/// Base class for Catalog module integration tests.
/// Configures `CatalogDbContext` targeting the local test database with automatic transactions rollbacks.
/// </summary>
public abstract class CatalogIntegrationTestBase : IntegrationTestBase
{
    internal CatalogDbContext DbContext => (CatalogDbContext)BaseDbContext;

    protected override DbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);

        return new CatalogDbContext(options, currentUserServiceMock.Object);
    }
}
