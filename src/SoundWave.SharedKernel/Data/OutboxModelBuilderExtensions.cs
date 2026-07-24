using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Data.Entities;

namespace SoundWave.SharedKernel.Data;

/// <summary>
/// Extension used by each module's DbContext (CatalogDbContext, IdentityDbContext, etc.)
/// to make EF aware of <see cref="OutboxMessage"/> so it can be tracked and saved in the
/// same transaction as business data — without each module owning the table or its migrations.
/// </summary>
public static class OutboxModelBuilderExtensions
{
    public static void ConfigureOutboxMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>()
            .ToTable(
                SharedConstants.Outbox.TableName,
                SharedConstants.Outbox.SchemaName,
                t => t.ExcludeFromMigrations()); // AppDbContext owns the migration, not this context
    }
}
