using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data.Config;

internal class TrackFileConfiguration : IEntityTypeConfiguration<TrackFile>
{
    public void Configure(EntityTypeBuilder<TrackFile> builder)
    {
        builder.ToTable("TrackFiles");

        builder.HasKey(tf => tf.TrackId);

        builder.Property(tf => tf.HlsPlaylistPath)
            .HasMaxLength(500);

        builder.Property(tf => tf.PreviewPlaylistPath)
            .HasMaxLength(500);

        builder.Property(tf => tf.RawFilePath)
            .HasMaxLength(500);

        builder.HasOne(tf => tf.Track)
            .WithOne(t => t.TrackFile)
            .HasForeignKey<TrackFile>(tf => tf.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
