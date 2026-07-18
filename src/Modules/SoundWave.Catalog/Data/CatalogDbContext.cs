using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data;

/// <summary>
/// Write-side EF Core context for the Catalog module.
/// All command (mutating) operations use this context.
/// Tracks changes, stamps audit fields, and persists to the Catalog schema.
/// </summary>
internal class CatalogDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public DbSet<Genre> Genres { get; set; } = default!;
    public DbSet<Artist> Artists { get; set; } = default!;
    public DbSet<ArtistAccountApproval> ArtistAccountApprovals { get; set; } = default!;
    public DbSet<Album> Albums { get; set; } = default!;
    public DbSet<Track> Tracks { get; set; } = default!;
    public DbSet<TrackFile> TrackFiles { get; set; } = default!;
    public DbSet<AlbumArtist> AlbumArtists { get; set; } = default!;
    public DbSet<TrackArtist> TrackArtists { get; set; } = default!;
    public DbSet<TrackGenre> TrackGenres { get; set; } = default!;
    public DbSet<AlbumGenre> AlbumGenres { get; set; } = default!;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scope all tables to the "Catalog" schema
        modelBuilder.HasDefaultSchema(Constants.SCHEMA_NAME);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUserId;
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = currentUserId;
                entry.Entity.UpdatedDate = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
