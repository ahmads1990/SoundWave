using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Data.Entities;

namespace SoundWave.SharedKernel.Data.Config;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(SharedConstants.Outbox.TableName, SharedConstants.Outbox.SchemaName);

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever(); // App generates Guid.CreateVersion7()

        builder.Property(m => m.Exchange)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(m => m.RoutingKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.Sent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.IsDeadLetter)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.ProcessedAt)
            .IsRequired(false);

        builder.Property(m => m.Error)
            .IsRequired(false)
            .HasMaxLength(1024);

        // Index to make the worker's polling query fast (filters on Sent + IsDeadLetter)
        builder.HasIndex(m => new { m.Sent, m.IsDeadLetter, m.RetryCount })
            .HasDatabaseName("IX_OutboxMessages_Sent_IsDeadLetter_RetryCount");
    }
}
