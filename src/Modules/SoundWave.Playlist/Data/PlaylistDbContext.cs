using Microsoft.EntityFrameworkCore;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Data;

/// <summary>
/// Write-side EF Core context for the Playlist module.
/// All command (mutating) operations use this context.
/// Tracks changes, stamps audit fields, and persists to the Playlist schema.
/// </summary>
internal class PlaylistDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public DbSet<Entities.Playlist> Playlists { get; set; } = default!;
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; } = default!;
    public DbSet<LikedTrack> LikedTracks { get; set; } = default!;
    public DbSet<LikedAlbum> LikedAlbums { get; set; } = default!;
    public DbSet<LikedPlaylist> LikedPlaylists { get; set; } = default!;
    public DbSet<PlaylistCollaborator> PlaylistCollaborators { get; set; } = default!;

    public PlaylistDbContext(DbContextOptions<PlaylistDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scope all tables to the "Playlist" schema
        modelBuilder.HasDefaultSchema(Constants.SCHEMA_NAME);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlaylistDbContext).Assembly);

        // MassTransit transactional outbox tables mapped to SharedKernel schema
        modelBuilder.AddMassTransitOutboxEntities();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUserId;
                entry.Entity.CreatedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = currentUserId;
                entry.Entity.UpdatedDate = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
