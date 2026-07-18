using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Data;

/// <summary>
/// Write-side EF Core context for the Catalog module.
/// All command (mutating) operations use this context.
/// Tracks changes, stamps audit fields, and persists to the Catalog schema.
/// </summary>
internal class CatalogDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    // DbSets will be added here as entities are introduced in Phase 1.4
    // e.g. public DbSet<Genre> Genres { get; set; } = default!;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scope all tables to the "Catalog" schema
        modelBuilder.HasDefaultSchema(Constants.SCHEMA_NAME);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUserId;
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = currentUserId;
                entry.Entity.UpdatedDate = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
