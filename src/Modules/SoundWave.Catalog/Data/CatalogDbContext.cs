using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Data;

/// <summary>
/// Write-side EF Core context for the Catalog module.
/// All command (mutating) operations use this context.
/// Tracks changes, stamps audit fields, and persists to the Catalog schema.
/// </summary>
internal class CatalogDbContext : BaseModuleDbContext
{
    protected override string SchemaName => Constants.SCHEMA_NAME;

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

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, ICurrentUserService currentUserService)
        : base(options, currentUserService)
    {
    }
}
