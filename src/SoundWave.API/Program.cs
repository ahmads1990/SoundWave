using Serilog;
using Hangfire;
using SoundWave.SharedKernel;
using SoundWave.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog configuration
SharedKernelExtensions.AddSerilogConfiguration(builder.Configuration);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOpenApi();

// Shared Kernel Wiring
builder.Services.AddSharedKernel(builder.Configuration, builder.Environment);

// MediatR — one line per module assembly
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(IdentityModule.Assembly);
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
