using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data.Config;

internal class ArtistAccountApprovalConfiguration : IEntityTypeConfiguration<ArtistAccountApproval>
{
    public void Configure(EntityTypeBuilder<ArtistAccountApproval> builder)
    {
        builder.ToTable("ArtistAccountApprovals");

        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.StageName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Bio)
            .HasMaxLength(1000);

        builder.Property(a => a.RejectionReason)
            .HasMaxLength(500);
    }
}
