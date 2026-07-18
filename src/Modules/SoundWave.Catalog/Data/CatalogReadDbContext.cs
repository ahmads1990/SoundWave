using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Entities;

using SoundWave.Catalog.Data.Entities;

namespace SoundWave.Catalog.Data;

/// <summary>
/// Read-only EF Core context for the Catalog module.
/// All query (read) operations use this context.
/// <para>
/// Enforces read-only access in two complementary ways:
/// <list type="number">
/// <item><description>
///   <see cref="QueryTrackingBehavior.NoTracking"/> is set globally — entities are never tracked, so the
///   change tracker cannot stage writes even if called accidentally.
/// </description></item>
/// <item><description>
///   <see cref="SaveChangesAsync"/> throws <see cref="InvalidOperationException"/> — any code path that
///   reaches <c>SaveChanges</c> on this context is a bug and will fail loudly at runtime.
/// </description></item>
/// </list>
/// </para>
/// To swap to a read replica later, register a second connection string and point
/// <c>ICatalogReadRepository&lt;T&gt;</c> → <c>CatalogReadRepository&lt;T&gt;</c> at this context.
/// Zero handler changes required.
/// </summary>
internal class CatalogReadDbContext : DbContext
{
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

    public CatalogReadDbContext(DbContextOptions<CatalogReadDbContext> options) : base(options) { }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Must match CatalogDbContext exactly so queries resolve to the same schema/tables
        modelBuilder.HasDefaultSchema(Constants.SCHEMA_NAME);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogReadDbContext).Assembly);
    }

    /// <summary>
    /// Always throws. This context is read-only by design.
    /// Command handlers must use <see cref="CatalogDbContext"/> instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"{nameof(CatalogReadDbContext)} is read-only. Use {nameof(CatalogDbContext)} for write operations.");
}
