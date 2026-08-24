using Microsoft.EntityFrameworkCore;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;

namespace SoundWave.Playlist.Data;

/// <summary>
/// Read-only EF Core context for the Playlist module.
/// All query (read) operations use this context.
/// Enforces NoTracking and throws on SaveChangesAsync.
/// </summary>
internal class PlaylistReadDbContext : DbContext
{
    public DbSet<Entities.Playlist> Playlists { get; set; } = default!;
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; } = default!;
    public DbSet<LikedTrack> LikedTracks { get; set; } = default!;
    public DbSet<LikedAlbum> LikedAlbums { get; set; } = default!;
    public DbSet<LikedPlaylist> LikedPlaylists { get; set; } = default!;
    public DbSet<PlaylistCollaborator> PlaylistCollaborators { get; set; } = default!;

    public PlaylistReadDbContext(DbContextOptions<PlaylistReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Must match PlaylistDbContext exactly so queries resolve to the same schema/tables
        modelBuilder.HasDefaultSchema(Constants.SCHEMA_NAME);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlaylistReadDbContext).Assembly);
    }

    /// <summary>
    /// Always throws. This context is read-only by design.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"{nameof(PlaylistReadDbContext)} is read-only. Use {nameof(PlaylistDbContext)} for write operations.");
}
