using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;

namespace SoundWave.Identity.Data.Config;

internal class AdminProfileConfiguration : IEntityTypeConfiguration<AdminProfile>
{
    public void Configure(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.ToTable("AdminProfiles", Constants.SCHEMA_NAME);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.CanApproveArtists)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CanLockUsers)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CanViewAuditLogs)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique index on UserId — one AdminProfile per User
        builder.HasIndex(a => a.UserId)
            .IsUnique();

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
