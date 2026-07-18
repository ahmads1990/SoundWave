using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Repository;

/// <summary>
/// Write-side repository implementation for the Catalog module.
/// Backed by <see cref="CatalogDbContext"/> — full EF tracking, supports all CRUD operations.
/// Inject <see cref="ICatalogRepository{TEntity}"/> in command handlers.
/// </summary>
/// <typeparam name="TEntity">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
internal class CatalogRepository<TEntity> : Repository<TEntity, CatalogDbContext>, ICatalogRepository<TEntity>
    where TEntity : BaseEntity
{
    public CatalogRepository(CatalogDbContext dbContext) : base(dbContext)
    {
    }
}
