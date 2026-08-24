using System.Reflection;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.Playlist.Data.Repository;
using SoundWave.Playlist.Messaging.Consumers;
using SoundWave.SharedKernel;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist;

/// <summary>
/// Assembly marker for the Playlist module.
/// Used for MediatR registration, FluentValidation scanning, and endpoint auto-discovery.
/// </summary>
public static class PlaylistModule
{
    public static Assembly Assembly => typeof(PlaylistModule).Assembly;

    /// <summary>
    /// Configures the transactional outbox on PlaylistDbContext for MassTransit.
    /// </summary>
    public static void ConfigureMassTransitOutbox(IBusRegistrationConfigurator configurator)
    {
        configurator.AddEntityFrameworkOutbox<PlaylistDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox();
        });
    }

    /// <summary>
    /// Registers MassTransit message consumers for the Playlist module.
    /// </summary>
    public static void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<UserRegisteredConsumer>();
    }

    /// <summary>
    /// Registers Playlist module services into the DI container.
    /// </summary>
    public static IServiceCollection AddPlaylistModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetDefaultConnectionString();

        // Write context — full EF tracking, used by command handlers
        services.AddDbContext<PlaylistDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Read context — NoTracking declared here, throws on SaveChanges, used by query handlers
        services.AddDbContext<PlaylistReadDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Repository registrations
        services.AddScoped(typeof(IPlaylistRepository<>), typeof(PlaylistRepository<>));
        services.AddScoped(typeof(IPlaylistReadRepository<>), typeof(PlaylistReadRepository<>));

        // Validators scanning
        services.AddValidatorsFromAssembly(Assembly, includeInternalTypes: true);

        return services;
    }

    /// <summary>
    /// Scans the Playlist assembly for all <see cref="IEndpoint"/> implementations and maps them.
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var endpoints = Assembly.GetTypes()
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(Activator.CreateInstance)
            .Cast<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.Map(app);
        }
    }
}
