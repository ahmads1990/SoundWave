using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Testing.Core;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected DbContext BaseDbContext { get; private set; } = null!;
    private IDbContextTransaction _transaction = null!;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static bool _databaseInitialized = false;

    /// <summary>
    /// Derived classes must implement this to create the specific DbContext using the provided connection string.
    /// This allows injecting mocks (e.g. ICurrentUserService) into the DbContext.
    /// </summary>
    protected abstract DbContext CreateDbContext(string connectionString);

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=SoundWaveIdentity_Test;Trusted_Connection=True;MultipleActiveResultSets=true";

        BaseDbContext = CreateDbContext(connectionString);

        if (!_databaseInitialized)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_databaseInitialized)
                {
                    await BaseDbContext.Database.EnsureDeletedAsync();
                    await BaseDbContext.Database.EnsureCreatedAsync();
                    _databaseInitialized = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Start a transaction — everything written in the test will be rolled back
        _transaction = await BaseDbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Roll back everything the test wrote — DB is exactly as it was before the test
        await _transaction.RollbackAsync();
        await BaseDbContext.DisposeAsync();
    }

    /// <summary>
    /// Helper: seed entities directly into the DB within the current transaction.
    /// Use this to set up preconditions for a test.
    /// </summary>
    protected async Task SeedAsync<T>(params T[] entities) where T : class
    {
        BaseDbContext.Set<T>().AddRange(entities);
        await BaseDbContext.SaveChangesAsync();
    }
}
