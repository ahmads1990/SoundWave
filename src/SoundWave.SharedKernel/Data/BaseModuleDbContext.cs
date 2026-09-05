using MassTransit;
using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Data;

/// <summary>
/// Abstract base EF Core context for write operations within a module.
/// Automatically handles:
/// - Setting the module's database schema (<see cref="SchemaName"/>)
/// - Applying entity configurations from the derived context's assembly
/// - Configuring MassTransit outbox entities in the shared schema
/// - Automatic audit stamping (CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) on <see cref="BaseEntity"/>
/// </summary>
public abstract class BaseModuleDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// The database schema name to which all tables of this module belong.
    /// </summary>
    protected abstract string SchemaName { get; }

    protected BaseModuleDbContext(
        DbContextOptions options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scope module tables to the module schema
        modelBuilder.HasDefaultSchema(SchemaName);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in the derived context's assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        // MassTransit transactional outbox tables mapped to SharedKernel schema
        modelBuilder.AddMassTransitOutboxEntities();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUserId;
                entry.Entity.CreatedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = currentUserId;
                entry.Entity.UpdatedDate = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
