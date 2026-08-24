using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;

namespace SoundWave.Playlist.Data.Config;

internal class LikedTrackConfiguration : IEntityTypeConfiguration<LikedTrack>
{
    public void Configure(EntityTypeBuilder<LikedTrack> builder)
    {
        builder.ToTable("LikedTracks", Constants.SCHEMA_NAME);

        builder.HasKey(lt => new { lt.UserId, lt.TrackId });

        builder.Property(lt => lt.LikedAt)
            .IsRequired();

        builder.HasIndex(lt => lt.UserId);
        builder.HasIndex(lt => lt.TrackId);
    }
}

internal class LikedAlbumConfiguration : IEntityTypeConfiguration<LikedAlbum>
{
    public void Configure(EntityTypeBuilder<LikedAlbum> builder)
    {
        builder.ToTable("LikedAlbums", Constants.SCHEMA_NAME);

        builder.HasKey(la => new { la.UserId, la.AlbumId });

        builder.Property(la => la.LikedAt)
            .IsRequired();

        builder.HasIndex(la => la.UserId);
        builder.HasIndex(la => la.AlbumId);
    }
}

internal class LikedPlaylistConfiguration : IEntityTypeConfiguration<LikedPlaylist>
{
    public void Configure(EntityTypeBuilder<LikedPlaylist> builder)
    {
        builder.ToTable("LikedPlaylists", Constants.SCHEMA_NAME);

        builder.HasKey(lp => new { lp.UserId, lp.PlaylistId });

        builder.Property(lp => lp.LikedAt)
            .IsRequired();

        builder.HasIndex(lp => lp.UserId);
        builder.HasIndex(lp => lp.PlaylistId);

        builder.HasOne(lp => lp.Playlist)
            .WithMany()
            .HasForeignKey(lp => lp.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class PlaylistCollaboratorConfiguration : IEntityTypeConfiguration<PlaylistCollaborator>
{
    public void Configure(EntityTypeBuilder<PlaylistCollaborator> builder)
    {
        builder.ToTable("PlaylistCollaborators", Constants.SCHEMA_NAME);

        builder.HasKey(pc => new { pc.PlaylistId, pc.UserId });

        builder.Property(pc => pc.AddedAt)
            .IsRequired();

        builder.Property(pc => pc.Role)
            .HasMaxLength(50);

        builder.HasIndex(pc => pc.PlaylistId);
        builder.HasIndex(pc => pc.UserId);
    }
}
