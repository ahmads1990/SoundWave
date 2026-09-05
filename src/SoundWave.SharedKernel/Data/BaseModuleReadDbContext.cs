using Microsoft.EntityFrameworkCore;

namespace SoundWave.SharedKernel.Data;

/// <summary>
/// Abstract base EF Core context for read-only query operations within a module.
/// Automatically handles:
/// - Setting the module's database schema (<see cref="SchemaName"/>)
/// - Applying entity configurations from the derived context's assembly
/// - Throwing on <see cref="SaveChangesAsync"/> to prevent accidental writes
/// </summary>
public abstract class BaseModuleReadDbContext : DbContext
{
    /// <summary>
    /// The database schema name to which all tables of this module belong.
    /// </summary>
    protected abstract string SchemaName { get; }

    protected BaseModuleReadDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Must match write context schema so queries resolve to the same schema/tables
        modelBuilder.HasDefaultSchema(SchemaName);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in the derived context's assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    /// <summary>
    /// Always throws. This context is read-only by design.
    /// Command handlers must use the write DbContext instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"{GetType().Name} is read-only. Use the write DbContext for mutating operations.");
}
