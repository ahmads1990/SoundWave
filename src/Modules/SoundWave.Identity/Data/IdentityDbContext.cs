using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Data;

internal class IdentityDbContext : DbContext
{
    #region Services

    private readonly ICurrentUserService _currentUserService;

    #endregion

    #region Entities

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<UserProfile> UserProfiles { get; set; } = default!;
    public DbSet<AdminProfile> AdminProfiles { get; set; } = default!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<Country> Countries { get; set; } = default!;

    #endregion

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scope all tables to the "Identity" schema
        modelBuilder.HasDefaultSchema(Constants.SCHEMA_NAME);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
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
