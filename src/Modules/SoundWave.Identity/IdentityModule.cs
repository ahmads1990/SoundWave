using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoundWave.Identity.Data;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel;
using SoundWave.SharedKernel.Common;
using System.Reflection;

namespace SoundWave.Identity;

/// <summary>
/// Assembly marker for the Identity module.
/// Used for MediatR registration, FluentValidation scanning, and endpoint auto-discovery.
/// </summary>
public static class IdentityModule
{
    public static Assembly Assembly => typeof(IdentityModule).Assembly;

    /// <summary>
    /// Registers identity module specific services to the DI container.
    /// </summary>
    public static IServiceCollection AddIdentityModuleServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetDefaultConnectionString();
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssembly(Assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IIdentityRepository<>), typeof(IdentityRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenHelper, TokenHelper>();
        return services;
    }

    /// <summary>
    /// Scans the Identity assembly for all <see cref="IEndpoint"/> implementations and maps them.
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
