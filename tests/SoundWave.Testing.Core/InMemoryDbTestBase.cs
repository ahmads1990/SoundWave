using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SoundWave.Testing.Core;

/// <summary>
/// Base class for tests that require an EF Core DbContext backed by an in-memory database.
/// Each test class gets an isolated, uniquely-named in-memory store.
/// </summary>
public abstract class InMemoryDbTestBase<TContext> : TestBase
    where TContext : DbContext
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(_dbName));
    }

    protected TContext CreateDbContext()
        => GetService<TContext>();
}
