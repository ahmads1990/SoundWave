using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Mapster;
using MapsterMapper;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Interfaces.Jobs;
using SoundWave.SharedKernel.Jobs;
using SoundWave.SharedKernel.Services;
using SoundWave.SharedKernel.Storage;
using SoundWave.SharedKernel.Storage.Consumers;
using SoundWave.SharedKernel.Storage.Services;
using SoundWave.SharedKernel.Validation;
using StackExchange.Redis;
using System.Reflection;
using System.Text;

namespace SoundWave.SharedKernel;

/// <summary>
/// Extension methods for configuring and bootstrapping the shared kernel infrastructure, services, and cross-cutting concerns.
/// </summary>
public static class SharedKernelExtensions
{
    /// <summary>
    /// Registers all shared kernel dependencies, database contexts, configurations, and core services.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SharedKernelDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetDefaultConnectionString(), sql =>
            {
                sql.MigrationsAssembly("SoundWave.API");
            });
        });

        services.AddSharedKernelServices();
        services.AddSharedKernelConfiguration(configuration);

        return services;
    }

    /// <summary>
    /// Retrieves the default application connection string from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The verified connection string.</returns>
    public static string GetDefaultConnectionString(this IConfiguration configuration)
    {
        return configuration.GetConnectionString(SharedConstants.DBConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{SharedConstants.DBConnectionStringName}' is missing.");
    }

    /// <summary>
    /// Registers shared application services, repositories, and background jobs.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSharedKernelServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICachingService, CachingService>();

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISendEmailJob, SendEmailJob>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Storage & Metadata repository
        services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        // Mime-Detective filtered content inspector
        services.AddSingleton<MimeDetective.IContentInspector>(_ => new MimeDetective.ContentInspectorBuilder
        {
            Definitions =
            [
                ..MimeDetective.Definitions.DefaultDefinitions.FileTypes.Images.All(),
                ..MimeDetective.Definitions.DefaultDefinitions.FileTypes.Audio.All(),
                ..MimeDetective.Definitions.DefaultDefinitions.FileTypes.Video.All()
            ]
        }.Build());

        // File validation & Image upload service
        services.AddScoped<FileMetadataValidator>();
        services.AddScoped<IImageUploadService, ImageUploadService>();

        // Hangfire storage cleanup & validation reconciliation jobs
        services.AddScoped<ICleanupStaleUploadsJob, CleanupStaleUploadsJob>();
        services.AddScoped<IReconcileStaleValidationsJob, ReconcileStaleValidationsJob>();

        return services;
    }

    /// <summary>
    /// Registers MassTransit message bus over RabbitMQ with retry policies and default consumers.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configureBus">Optional delegate to register additional module-specific consumers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMassTransitBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        var rabbitConfig = configuration.GetSection(nameof(RabbitMqConfig)).Get<RabbitMqConfig>()
            ?? throw new InvalidOperationException("RabbitMqConfig section is missing.");

        services.AddMassTransit(x =>
        {
            // Register shared storage notifications consumer
            x.AddConsumer<BucketNotificationConsumer>();

            configureBus?.Invoke(x);

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitConfig.HostName, (ushort)rabbitConfig.Port, rabbitConfig.VirtualHost, h =>
                {
                    h.Username(rabbitConfig.UserName);
                    h.Password(rabbitConfig.Password);
                });

                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: rabbitConfig.Retry.RetryLimit,
                    minInterval: TimeSpan.FromSeconds(rabbitConfig.Retry.MinIntervalSeconds),
                    maxInterval: TimeSpan.FromSeconds(rabbitConfig.Retry.MaxIntervalSeconds),
                    intervalDelta: TimeSpan.FromSeconds(rabbitConfig.Retry.IntervalDeltaSeconds)
                ));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }

    /// <summary>
    /// Registers strongly-typed configuration options and third-party infrastructure components.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSharedKernelConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtConfig>(configuration.GetSection(SharedConstants.JwtConfigSectionName));
        services.Configure<SMTPConfig>(configuration.GetSection(nameof(SMTPConfig)));
        services.Configure<RabbitMqConfig>(configuration.GetSection(nameof(RabbitMqConfig)));

        // Storage & job configs
        services.Configure<ObjectStorageConfig>(configuration.GetSection(SharedConstants.ObjectStorageConfigSectionName));
        services.Configure<CleanupStaleUploadsConfig>(configuration.GetSection(CleanupStaleUploadsConfig.SectionPath));
        services.Configure<ReconcileStaleValidationsConfig>(configuration.GetSection(ReconcileStaleValidationsConfig.SectionPath));

        services.AddMapsterConfiguration();
        services.AddRedisConfiguration(configuration);
        services.AddHangfireConfiguration(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddObjectStorageConfiguration(configuration);

        return services;
    }

    #region Redis Configuration

    /// <summary>
    /// Registers StackExchange.Redis connection multiplexer and caching services.
    /// </summary>
    public static IServiceCollection AddRedisConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
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

    #endregion

    #region Hangfire Configuration

    /// <summary>
    /// Registers Hangfire server and SQL Server persistence storage.
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetDefaultConnectionString();
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

    /// <summary>
    /// Registers JWT Bearer authentication and role-based authorization policies (Admin, Artist, Listener).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection(SharedConstants.JwtConfigSectionName).Get<JwtConfig>();
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireAdminRole, policy =>
                policy.RequireRole(nameof(UserRole.Admin)));

            options.AddPolicy(Policies.RequireArtistRole, policy =>
                policy.RequireRole(nameof(UserRole.Artist)));

            options.AddPolicy(Policies.RequireListenerRole, policy =>
                policy.RequireRole(nameof(UserRole.Listener)));
        });

        return services;
    }

    #endregion

    #region Mapster Configuration

    /// <summary>
    /// Registers Mapster mapper instance and scans executing assembly for mapping profiles.
    /// </summary>
    public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    #endregion

    #region Serilog Configuration

    /// <summary>
    /// Configures global Serilog logging sinks (Console, Seq, Rolling File) from configuration.
    /// </summary>
    public static void AddSerilogConfiguration(IConfiguration configuration)
    {
        var writeToSeq = configuration.GetValue<bool>("WriteToSeq");

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

    #region Object Storage Configuration

    /// <summary>
    /// Registers AWS S3 client and hosted configurator for bucket setup, policies, and lifecycle rules.
    /// </summary>
    public static IServiceCollection AddObjectStorageConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var storageConfig = configuration.GetSection(SharedConstants.ObjectStorageConfigSectionName).Get<ObjectStorageConfig>();
        if (storageConfig == null) throw new InvalidOperationException("Object storage config is missing");

        var credentials = new BasicAWSCredentials(storageConfig.Username, storageConfig.Password);
        var config = new AmazonS3Config
        {
            ServiceURL = storageConfig.ServiceUrl,
            ForcePathStyle = storageConfig.ForcePathStyle,
            UseHttp = !storageConfig.ServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        };

        services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(credentials, config));
        services.AddHostedService<ObjectStorageConfigurator>();

        return services;
    }

    #endregion

    #region Recurring Jobs

    /// <summary>
    /// Registers all SharedKernel recurring background jobs with Hangfire.
    /// </summary>
    /// <param name="app">The application builder pipeline.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder AddSharedKernelRecurringJobs(this IApplicationBuilder app, IConfiguration configuration)
    {
        AddCleanupStaleUploadsJob(configuration);
        AddReconcileStaleValidationsJob(configuration);
        return app;
    }

    /// <summary>
    /// Schedules or updates the stale upload cleanup recurring job in Hangfire.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    public static void AddCleanupStaleUploadsJob(IConfiguration configuration)
    {
        var jobConfig = configuration.GetSection(CleanupStaleUploadsConfig.SectionPath).Get<CleanupStaleUploadsConfig>()
            ?? new CleanupStaleUploadsConfig();

        RecurringJob.AddOrUpdate<ICleanupStaleUploadsJob>(
            jobConfig.JobId,
            job => job.ExecuteAsync(CancellationToken.None),
            jobConfig.CronExpression);
    }

    /// <summary>
    /// Schedules or updates the stale validation reconciliation scanner recurring job in Hangfire.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    public static void AddReconcileStaleValidationsJob(IConfiguration configuration)
    {
        var jobConfig = configuration.GetSection(ReconcileStaleValidationsConfig.SectionPath).Get<ReconcileStaleValidationsConfig>()
            ?? new ReconcileStaleValidationsConfig();

        RecurringJob.AddOrUpdate<IReconcileStaleValidationsJob>(
            jobConfig.JobId,
            job => job.ExecuteAsync(CancellationToken.None),
            jobConfig.CronExpression);
    }

    #endregion
}
