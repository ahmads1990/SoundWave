using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SoundWave.SharedKernel.Common;

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
    public static IServiceCollection AddIdentityModuleServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(Data.IIdentityRepository<>), typeof(Data.IdentityRepository<>));
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
