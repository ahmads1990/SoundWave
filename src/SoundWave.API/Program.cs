using FluentValidation;
using Hangfire;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SoundWave.API.Data;
using SoundWave.Identity;
using SoundWave.SharedKernel;
using SoundWave.SharedKernel.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog configuration
SharedKernelExtensions.AddSerilogConfiguration(builder.Configuration);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOpenApi();

// Shared Kernel Wiring (Redis, Hangfire, JWT, config options)
builder.Services.AddSharedKernel(builder.Configuration, builder.Environment);

// Identity Module Wiring
builder.Services.AddIdentityModuleServices();

// ── Mapster — scan module assemblies for IRegister mapping configs ───────────
TypeAdapterConfig.GlobalSettings.Scan(IdentityModule.Assembly);

// ── AppDbContext — single shared database, owns all migrations ───────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

// ── FluentValidation — one line per module assembly ─────────────────────────
builder.Services.AddValidatorsFromAssembly(IdentityModule.Assembly);

// ── MediatR — one line per module assembly ───────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(IdentityModule.Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));      // First: wraps full pipeline
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));   // Second: validates before handler
});

var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

// Minimal API — module endpoints
IdentityModule.MapEndpoints(app);

app.Run();
