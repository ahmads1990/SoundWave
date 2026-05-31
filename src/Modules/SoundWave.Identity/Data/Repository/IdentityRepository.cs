using SoundWave.Identity.Data.IRepository;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Identity.Data.Repository;

/// <summary>
/// Identity module specific repository implementation using IdentityDbContext.
/// </summary>
/// <typeparam name="TEntity">The entity type which derives from BaseEntity.</typeparam>
internal class IdentityRepository<TEntity> : Repository<TEntity, IdentityDbContext>, IIdentityRepository<TEntity>
    where TEntity : BaseEntity
{
    public IdentityRepository(IdentityDbContext dbContext) : base(dbContext)
    {
    }
}
