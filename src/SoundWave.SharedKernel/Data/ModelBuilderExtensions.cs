using MassTransit;
using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Data;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures MassTransit transactional inbox and outbox entities (InboxState, OutboxMessages, OutboxState)
    /// mapped to the specified schema (defaults to <see cref="SharedConstants.Outbox.SchemaName"/>).
    /// </summary>
    /// <param name="modelBuilder">The EF Core <see cref="ModelBuilder"/> instance.</param>
    /// <param name="schema">The database schema name to map outbox tables to.</param>
    /// <returns>The configured <see cref="ModelBuilder"/>.</returns>
    public static ModelBuilder AddMassTransitOutboxEntities(
        this ModelBuilder modelBuilder,
        string schema = SharedConstants.Outbox.SchemaName)
    {
        modelBuilder.AddInboxStateEntity(b => b.ToTable("InboxState", schema));
        modelBuilder.AddOutboxMessageEntity(b => b.ToTable("OutboxMessages", schema));
        modelBuilder.AddOutboxStateEntity(b => b.ToTable("OutboxState", schema));

        return modelBuilder;
    }
}
