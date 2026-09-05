using Microsoft.EntityFrameworkCore;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Data;

namespace SoundWave.Playlist.Data;

/// <summary>
/// Read-only EF Core context for the Playlist module.
/// All query (read) operations use this context.
/// Inherits from <see cref="BaseModuleReadDbContext"/> which automatically scopes the schema,
/// applies entity configurations, and throws on SaveChangesAsync.
/// </summary>
internal class PlaylistReadDbContext : BaseModuleReadDbContext
{
    protected override string SchemaName => Constants.SCHEMA_NAME;

    public DbSet<Entities.Playlist> Playlists { get; set; } = default!;
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; } = default!;
    public DbSet<LikedTrack> LikedTracks { get; set; } = default!;
    public DbSet<LikedAlbum> LikedAlbums { get; set; } = default!;
    public DbSet<LikedPlaylist> LikedPlaylists { get; set; } = default!;
    public DbSet<PlaylistCollaborator> PlaylistCollaborators { get; set; } = default!;

    public PlaylistReadDbContext(DbContextOptions<PlaylistReadDbContext> options) : base(options) { }
}
