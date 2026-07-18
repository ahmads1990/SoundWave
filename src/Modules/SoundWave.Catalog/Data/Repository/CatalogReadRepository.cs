using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Entities;
using System.Linq.Expressions;

namespace SoundWave.Catalog.Data.Repository;

/// <summary>
/// Read-only repository implementation for the Catalog module.
/// Backed by <see cref="CatalogReadDbContext"/> — <c>QueryTrackingBehavior.NoTracking</c> globally,
/// <c>SaveChanges</c> throws. Exposes read methods only.
/// Inject <see cref="ICatalogReadRepository{TEntity}"/> in query handlers.
/// </summary>
/// <typeparam name="TEntity">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
internal class CatalogReadRepository<TEntity> : ICatalogReadRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly DbSet<TEntity> _dbSet;

    public CatalogReadRepository(CatalogReadDbContext dbContext)
    {
        _dbSet = dbContext.Set<TEntity>();
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> GetAll()
        => _dbSet.Where(e => !e.IsDeleted);

    /// <inheritdoc/>
    public IQueryable<TEntity> GetByID(Guid id)
        => GetByCondition(e => e.Id == id);

    /// <inheritdoc/>
    public Task<TEntity?> GetByID(Guid id, CancellationToken cancellationToken = default)
        => GetAll().Where(e => e.Id == id).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public IQueryable<TEntity> GetByCondition(Expression<Func<TEntity, bool>> expression)
        => GetAll().Where(expression);

    /// <inheritdoc/>
    public Task<TEntity?> GetByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        => GetAll().Where(expression).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<bool> CheckExistsByID(Guid id, CancellationToken cancellationToken = default)
        => GetAll().AnyAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> CheckExistsByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        => GetAll().AnyAsync(expression, cancellationToken);
}
