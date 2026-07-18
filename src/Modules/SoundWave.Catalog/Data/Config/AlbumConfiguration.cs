using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data.Config;

internal class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("Albums");

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);
    }
}
