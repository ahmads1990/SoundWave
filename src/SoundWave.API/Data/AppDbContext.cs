using MassTransit;
using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog;
using SoundWave.Identity.Data.Seed;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

using SoundWave.SharedKernel.Data;

namespace SoundWave.API.Data;

// Single shared context — owns all EF migrations for the entire solution.
// Each module's IEntityTypeConfiguration<T> files declare their own schema via ToTable("X", "Schema").
public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit outbox/inbox tables in SharedKernel schema
        modelBuilder.AddMassTransitOutboxEntities();

        // Identity module
        modelBuilder.ApplyConfigurationsFromAssembly(Identity.IdentityModule.Assembly);
        IdentitySeedData.Seed(modelBuilder);

        // Catalog module
        modelBuilder.ApplyConfigurationsFromAssembly(CatalogModule.Assembly);
 
        // Streaming module — uncomment in Phase 2
        // modelBuilder.ApplyConfigurationsFromAssembly(StreamingModule.Assembly);

        // Playlist module — uncomment in Phase 1.6
        // modelBuilder.ApplyConfigurationsFromAssembly(PlaylistModule.Assembly);

        // Social module — uncomment in Phase 3
        // modelBuilder.ApplyConfigurationsFromAssembly(SocialModule.Assembly);

        // Analytics module — uncomment in Phase 4
        // modelBuilder.ApplyConfigurationsFromAssembly(AnalyticsModule.Assembly);
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
