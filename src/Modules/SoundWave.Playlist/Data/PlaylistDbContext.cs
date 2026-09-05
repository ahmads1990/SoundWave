using Microsoft.EntityFrameworkCore;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Data;

/// <summary>
/// Write-side EF Core context for the Playlist module.
/// All command (mutating) operations use this context.
/// Tracks changes, stamps audit fields, and persists to the Playlist schema.
/// </summary>
internal class PlaylistDbContext : BaseModuleDbContext
{
    protected override string SchemaName => Constants.SCHEMA_NAME;

    public DbSet<Entities.Playlist> Playlists { get; set; } = default!;
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; } = default!;
    public DbSet<LikedTrack> LikedTracks { get; set; } = default!;
    public DbSet<LikedAlbum> LikedAlbums { get; set; } = default!;
    public DbSet<LikedPlaylist> LikedPlaylists { get; set; } = default!;
    public DbSet<PlaylistCollaborator> PlaylistCollaborators { get; set; } = default!;

    public PlaylistDbContext(DbContextOptions<PlaylistDbContext> options, ICurrentUserService currentUserService)
        : base(options, currentUserService)
    {
    }
}
