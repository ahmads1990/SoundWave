using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Data.Repository;
using SoundWave.SharedKernel;
using SoundWave.SharedKernel.Common;
using System.Reflection;

namespace SoundWave.Catalog;

/// <summary>
/// Assembly marker for the Catalog module.
/// Used for MediatR registration, FluentValidation scanning, and endpoint auto-discovery.
/// </summary>
public static class CatalogModule
{
    public static Assembly Assembly => typeof(CatalogModule).Assembly;

    /// <summary>
    /// Configures the transactional outbox on CatalogDbContext for MassTransit.
    /// </summary>
    public static void ConfigureMassTransitOutbox(IBusRegistrationConfigurator configurator)
    {
        configurator.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox();
        });
    }

    /// <summary>
    /// Registers all MassTransit consumers for the Catalog module.
    /// </summary>
    public static void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<Messaging.Consumers.ArtistApplicationSubmittedEmailConsumer>();
        configurator.AddConsumer<Messaging.Consumers.ArtistApplicationApprovedEmailConsumer>();
        configurator.AddConsumer<Messaging.Consumers.ArtistApplicationRejectedEmailConsumer>();
    }

    /// <summary>
    /// Registers Catalog module services into the DI container.
    /// </summary>
    /// <remarks>
    /// Both <see cref="CatalogDbContext"/> (write) and <see cref="CatalogReadDbContext"/> (read)
    /// currently share the same connection string. To point the read context at a read replica,
    /// replace <c>GetDefaultConnectionString()</c> here with a separate config key — zero handler changes needed.
    /// </remarks>
    public static IServiceCollection AddCatalogModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetDefaultConnectionString();

        // Write context — full EF tracking, used by command handlers
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Read context — NoTracking declared here at registration, throws on SaveChanges, used by query handlers
        services.AddDbContext<CatalogReadDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Repository registrations
        services.AddScoped(typeof(ICatalogRepository<>), typeof(CatalogRepository<>));
        services.AddScoped(typeof(ICatalogReadRepository<>), typeof(CatalogReadRepository<>));

        return services;
    }

    /// <summary>
    /// Scans the Catalog assembly for all <see cref="IEndpoint"/> implementations and maps them.
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var endpoints = Assembly.GetTypes()
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(Activator.CreateInstance)
            .Cast<IEndpoint>();

        foreach (var endpoint in endpoints)
            endpoint.Map(app);
    }
}
