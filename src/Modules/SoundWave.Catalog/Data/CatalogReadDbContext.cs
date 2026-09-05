using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Data;

namespace SoundWave.Catalog.Data;

/// <summary>
/// Read-only EF Core context for the Catalog module.
/// All query (read) operations use this context.
/// Inherits from <see cref="BaseModuleReadDbContext"/> which automatically scopes the schema,
/// applies entity configurations, and throws on SaveChangesAsync.
/// </summary>
internal class CatalogReadDbContext : BaseModuleReadDbContext
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

    public CatalogReadDbContext(DbContextOptions<CatalogReadDbContext> options) : base(options) { }
}
