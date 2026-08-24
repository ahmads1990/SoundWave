using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.Playlist.Data.Repository;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.Testing.Core;

namespace SoundWave.Playlist.Tests;

/// <summary>
/// Base class for Playlist module integration tests.
/// Configures `PlaylistDbContext` targeting the local test database with automatic transaction rollbacks.
/// </summary>
public abstract class PlaylistIntegrationTestBase : IntegrationTestBase
{
    internal PlaylistDbContext DbContext => (PlaylistDbContext)BaseDbContext;

    /// <summary>
    /// Creates a <see cref="PlaylistReadDbContext"/> instance sharing the active test transaction.
    /// </summary>
    internal PlaylistReadDbContext CreateReadDbContext()
    {
        var connection = DbContext.Database.GetDbConnection();
        var options = new DbContextOptionsBuilder<PlaylistReadDbContext>()
            .UseSqlServer(connection)
            .Options;

        var readContext = new PlaylistReadDbContext(options);

        var currentTransaction = DbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            readContext.Database.UseTransaction(currentTransaction.GetDbTransaction());
        }

        return readContext;
    }

    internal IPlaylistRepository<TEntity> CreateRepository<TEntity>() where TEntity : SoundWave.SharedKernel.Entities.BaseEntity
        => new PlaylistRepository<TEntity>(DbContext);

    internal IPlaylistReadRepository<TEntity> CreateReadRepository<TEntity>() where TEntity : SoundWave.SharedKernel.Entities.BaseEntity
        => new PlaylistReadRepository<TEntity>(CreateReadDbContext());

    protected override DbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PlaylistDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);

        return new PlaylistDbContext(options, currentUserServiceMock.Object);
    }
}
