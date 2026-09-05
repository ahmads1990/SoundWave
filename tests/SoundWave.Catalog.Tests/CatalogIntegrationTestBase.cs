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

    protected override async Task OnDatabaseCreatedAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Auth')
                EXEC('CREATE SCHEMA [Auth]');
            IF OBJECT_ID(N'Auth.Users', N'U') IS NULL
                CREATE TABLE [Auth].[Users] (
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [Email] nvarchar(256) NOT NULL
                );
            IF OBJECT_ID(N'Auth.UserProfiles', N'U') IS NULL
                CREATE TABLE [Auth].[UserProfiles] (
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [UserId] uniqueidentifier NOT NULL,
                    [FirstName] nvarchar(100) NOT NULL,
                    [LastName] nvarchar(100) NOT NULL
                );
        ");
    }

    /// <summary>
    /// Seeds user and profile lookup records in the Auth schema within the active test transaction.
    /// </summary>
    internal async Task SeedUserLookupAsync(Guid userId, string email, string firstName, string lastName)
    {
        await DbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO [Auth].[Users] ([Id], [Email]) VALUES ({0}, {1});",
            userId, email);

        await DbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO [Auth].[UserProfiles] ([Id], [UserId], [FirstName], [LastName]) VALUES ({0}, {1}, {2}, {3});",
            Guid.NewGuid(), userId, firstName, lastName);
    }

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
