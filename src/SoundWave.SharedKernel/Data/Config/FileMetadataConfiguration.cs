using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.SharedKernel.Data.Config;

/// <summary>
/// Entity Framework Core configuration for mapping the <see cref="FileMetadata"/> entity to the database schema.
/// </summary>
public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.ToTable("FileMetadata", SharedConstants.SchemaName);

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(f => f.StorageKey)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(f => f.StorageKey)
            .IsUnique();

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(f => f.SizeBytes)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(f => f.Type)
            .IsRequired();

        builder.Property(f => f.ETag)
            .HasMaxLength(128);

        builder.Property(f => f.FailureReason)
            .HasMaxLength(1024);

        builder.Property(f => f.ConfirmedAt);

        builder.Property(f => f.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(f => new { f.Status, f.CreatedDate });
        builder.HasIndex(f => new { f.IsDeleted, f.UpdatedDate });
    }
}
