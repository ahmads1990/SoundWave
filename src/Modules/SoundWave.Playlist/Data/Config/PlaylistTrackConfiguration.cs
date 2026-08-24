using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;

namespace SoundWave.Playlist.Data.Config;

internal class PlaylistTrackConfiguration : IEntityTypeConfiguration<PlaylistTrack>
{
    public void Configure(EntityTypeBuilder<PlaylistTrack> builder)
    {
        builder.ToTable("PlaylistTracks", Constants.SCHEMA_NAME);

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Position)
            .IsRequired();

        builder.Property(pt => pt.AddedAt)
            .IsRequired();

        builder.Property(pt => pt.AddedBy)
            .IsRequired();

        // Index on PlaylistId + Position for fast ordered retrieval
        builder.HasIndex(pt => new { pt.PlaylistId, pt.Position });

        // Index on PlaylistId + TrackId for membership checks
        builder.HasIndex(pt => new { pt.PlaylistId, pt.TrackId });

        builder.HasIndex(pt => pt.IsDeleted);
    }
}
