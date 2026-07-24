using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Data.Entities;

namespace SoundWave.SharedKernel.Data;

public class SharedDbContext : DbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = default!;

    public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply the OutboxMessage schema/table mapping (SharedKernel.OutboxMessages)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SharedDbContext).Assembly);
    }
}
