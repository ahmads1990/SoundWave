using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Seed;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Data;

internal class IdentityDbContext : BaseModuleDbContext
{
    protected override string SchemaName => Constants.SCHEMA_NAME;

    #region Entities

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<UserProfile> UserProfiles { get; set; } = default!;
    public DbSet<AdminProfile> AdminProfiles { get; set; } = default!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<Country> Countries { get; set; } = default!;

    #endregion

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICurrentUserService currentUserService)
        : base(options, currentUserService)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed initial data
        IdentitySeedData.Seed(modelBuilder);
    }
}
