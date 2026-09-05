using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.Entities.Lookups;
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

    /// <summary>
    /// Read-only cross-module lookup for Auth.Users.
    /// </summary>
    public DbSet<UserLookup> Users { get; set; } = default!;

    /// <summary>
    /// Read-only cross-module lookup for Auth.UserProfiles.
    /// </summary>
    public DbSet<UserProfileLookup> UserProfiles { get; set; } = default!;

    public CatalogReadDbContext(DbContextOptions<CatalogReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserLookup>(builder =>
        {
            builder.ToTable("Users", "Auth");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.HasOne(u => u.Profile)
                .WithOne()
                .HasForeignKey<UserProfileLookup>(p => p.UserId);
        });

        modelBuilder.Entity<UserProfileLookup>(builder =>
        {
            builder.ToTable("UserProfiles", "Auth");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.FirstName).HasMaxLength(100);
            builder.Property(p => p.LastName).HasMaxLength(100);
        });
    }
}
