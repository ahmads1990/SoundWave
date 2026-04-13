using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Services;
using StackExchange.Redis;
using System.Text;

namespace SoundWave.SharedKernel;

public static class SharedKernelExtensions
{
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        services.AddSharedKernelServices();
        services.AddSharedKernelConfiguration(configuration, env);
        services.AddJwtAuthentication(configuration);

        return services;
    }

    public static IServiceCollection AddSharedKernelServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddSharedKernelConfiguration(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        services.Configure<JwtConfig>(configuration.GetSection(Constants.JwtConfigSectionName));
        services.Configure<SMTPConfig>(configuration.GetSection(nameof(SMTPConfig)));

        services.AddCacheConfiguration(configuration);
        services.AddHangfireConfiguration(configuration);

        return services;
    }

    public static IServiceCollection AddCacheConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Distributed Redis cache via StackExchange.Redis
        var settings = configuration.GetSection(nameof(RedisConfig)).Get<RedisConfig>();
        var options = new ConfigurationOptions
        {
            EndPoints = { { settings!.Host, settings.Port } },
            User = settings.User,
            Password = settings.Password,
            Ssl = settings.Ssl,
            AbortOnConnectFail = settings.AbortOnConnectFail
        };
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(options));
        services.AddSingleton<ICachingService, CachingService>();

        return services;
    }

    #region Hangfire Configuration

    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(Constants.DBConnectionStringName);
        // Add Hangfire services
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString));

        services.AddHangfireServer();

        return services;
    }

    #endregion

    #region JWT Authentication

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection(Constants.JwtConfigSectionName).Get<JwtConfig>();
        if (jwtConfig == null) throw new InvalidOperationException("JwtConfig is missing");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key)),
            };
        });

        return services;
    }

    #endregion

    #region Serilog Configuration

    public static void AddSerilogConfiguration(IConfiguration configuration)
    {
        var writeToSeq = configuration.GetValue<bool>("WriteToSeq");

        // Build Serilog: enrichers and levels come from appsettings; sinks are added in code
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}"
            );

        if (writeToSeq)
        {
            var seqUrl = configuration["SeqUrl"] ?? "http://localhost:5341";
            loggerConfig = loggerConfig.WriteTo.Seq(seqUrl);
        }
        else
        {
            loggerConfig = loggerConfig.WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}"
            );
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    #endregion
}

