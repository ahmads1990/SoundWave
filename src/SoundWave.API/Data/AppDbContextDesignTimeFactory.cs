using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.API.Data;

// Design-time factory used exclusively by EF Core tooling.
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionString = "Data Source=.;Database=SoundWaveDB;Integrated Security=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(ConnectionString, sql =>
            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name));

        return new AppDbContext(optionsBuilder.Options, new MigrationsCurrentUserService());
    }
}

// Stub ICurrentUserService used only by the design-time factory.
file class MigrationsCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public bool IsAuthenticated => false;
}
