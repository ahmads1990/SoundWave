using Hangfire;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using SoundWave.API.Data;
using SoundWave.API.Middlewares;
using SoundWave.Catalog;
using SoundWave.Catalog.Data;
using SoundWave.Identity;
using SoundWave.Playlist;
using SoundWave.SharedKernel;
using SoundWave.SharedKernel.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog configuration
SharedKernelExtensions.AddSerilogConfiguration(builder.Configuration);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        // 1. Enrich enum schemas with their values and names (e.g., 0 = Male, 1 = Female, etc.)
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            var enumValues = Enum.GetValues(context.JsonTypeInfo.Type);
            var descriptions = new List<string>();
            foreach (var val in enumValues)
            {
                descriptions.Add($"{(int)val} = {val}");
            }
            schema.Description = (schema.Description ?? "") + " (" + string.Join(", ", descriptions) + ")";
        }

        // 2. Force int32 / C# int properties to show as integer (avoiding string-with-pattern fallback)
        if (context.JsonTypeInfo.Type == typeof(int) || context.JsonTypeInfo.Type == typeof(int?))
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.Integer;
            schema.Format = "int32";
        }

        return Task.CompletedTask;
    });
});

// Shared Kernel Wiring (Redis, Hangfire, JWT, config options)
builder.Services.AddSharedKernel(builder.Configuration);

// Identity Module Wiring
builder.Services.AddIdentityModuleServices(builder.Configuration);

// Catalog Module Wiring
builder.Services.AddCatalogModuleServices(builder.Configuration);

// Playlist Module Wiring
builder.Services.AddPlaylistModuleServices(builder.Configuration);

// ── Mapster — scan module assemblies for IRegister mapping configs ───────────
TypeAdapterConfig.GlobalSettings.Scan(IdentityModule.Assembly, CatalogModule.Assembly, PlaylistModule.Assembly);

// ── AppDbContext — single shared database, owns all migrations ───────────────
var connectionString = builder.Configuration.GetDefaultConnectionString();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

// ── MassTransit — RabbitMQ transport + module outboxes + consumers ───────────
builder.Services.AddMassTransitBus(builder.Configuration, x =>
{
    CatalogModule.ConfigureMassTransitOutbox(x);
    PlaylistModule.ConfigureMassTransitOutbox(x);
    IdentityModule.RegisterConsumers(x);
    CatalogModule.RegisterConsumers(x);
    PlaylistModule.RegisterConsumers(x);
});

// ── MediatR — one line per module assembly ───────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(IdentityModule.Assembly);
    cfg.RegisterServicesFromAssembly(CatalogModule.Assembly);
    cfg.RegisterServicesFromAssembly(PlaylistModule.Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));      // First: wraps full pipeline
    // cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));   // Disabled: business validation moved to handlers
});


var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TokenBlacklistMiddleware>();

app.UseHangfireDashboard();

// Minimal API — module endpoints under global /api group
var api = app.MapGroup("api");
IdentityModule.MapEndpoints(api);
CatalogModule.MapEndpoints(api);
PlaylistModule.MapEndpoints(api);

app.Run();
