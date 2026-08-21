using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Data.Repository;
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

    /// <summary>
    /// Creates a <see cref="CatalogReadDbContext"/> instance sharing the active test transaction.
    /// </summary>
    internal CatalogReadDbContext CreateReadDbContext()
    {
        var connection = DbContext.Database.GetDbConnection();
        var options = new DbContextOptionsBuilder<CatalogReadDbContext>()
            .UseSqlServer(connection)
            .Options;

        var readContext = new CatalogReadDbContext(options);

        var currentTransaction = DbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            readContext.Database.UseTransaction(currentTransaction.GetDbTransaction());
        }

        return readContext;
    }

    internal ICatalogRepository<TEntity> CreateRepository<TEntity>() where TEntity : SoundWave.SharedKernel.Entities.BaseEntity
        => new CatalogRepository<TEntity>(DbContext);

    internal ICatalogReadRepository<TEntity> CreateReadRepository<TEntity>() where TEntity : SoundWave.SharedKernel.Entities.BaseEntity
        => new CatalogReadRepository<TEntity>(CreateReadDbContext());

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
