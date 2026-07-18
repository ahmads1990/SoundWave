using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data.Config;

internal class AlbumGenreConfiguration : IEntityTypeConfiguration<AlbumGenre>
{
    public void Configure(EntityTypeBuilder<AlbumGenre> builder)
    {
        builder.ToTable("AlbumGenres");

        builder.HasKey(ag => new { ag.AlbumId, ag.GenreId });

        builder.HasOne(ag => ag.Album)
            .WithMany(a => a.AlbumGenres)
            .HasForeignKey(ag => ag.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ag => ag.Genre)
            .WithMany(g => g.AlbumGenres)
            .HasForeignKey(ag => ag.GenreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
