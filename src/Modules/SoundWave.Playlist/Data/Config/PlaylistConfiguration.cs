using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;

namespace SoundWave.Playlist.Data.Config;

internal class PlaylistConfiguration : IEntityTypeConfiguration<Entities.Playlist>
{
    public void Configure(EntityTypeBuilder<Entities.Playlist> builder)
    {
        builder.ToTable("Playlists", Constants.SCHEMA_NAME);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Visibility)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(p => p.IsSystem)
            .HasDefaultValue(false);

        builder.Property(p => p.TrackCount)
            .HasDefaultValue(0);

        builder.Property(p => p.TotalDurationSeconds)
            .HasDefaultValue(0);

        builder.Property(p => p.FollowerCount)
            .HasDefaultValue(0);

        builder.HasIndex(p => p.OwnerId);
        builder.HasIndex(p => new { p.OwnerId, p.IsSystem });
        builder.HasIndex(p => p.Visibility);
        builder.HasIndex(p => p.IsDeleted);

        builder.HasMany(p => p.PlaylistTracks)
            .WithOne(pt => pt.Playlist)
            .HasForeignKey(pt => pt.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Collaborators)
            .WithOne(c => c.Playlist)
            .HasForeignKey(c => c.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
