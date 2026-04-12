using Microsoft.Extensions.DependencyInjection;

namespace SoundWave.Testing.Core;

/// <summary>
/// Base class for unit tests that need a configured DI container.
/// Provides a fresh <see cref="IServiceCollection"/> and helper to build a provider.
/// </summary>
public abstract class TestBase
{
    protected IServiceCollection Services { get; }

    protected TestBase()
    {
        Services = new ServiceCollection();
        ConfigureServices(Services);
    }

    /// <summary>
    /// Override to register additional services for your test class.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    protected IServiceProvider BuildServiceProvider() => Services.BuildServiceProvider();

    protected T GetService<T>() where T : notnull
        => BuildServiceProvider().GetRequiredService<T>();
}
