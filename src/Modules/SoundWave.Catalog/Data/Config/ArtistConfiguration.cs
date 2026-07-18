using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data.Config;

internal class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("Artists");

        builder.HasIndex(a => a.UserId)
            .IsUnique();

        builder.Property(a => a.StageName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Bio)
            .HasMaxLength(1000);
    }
}
