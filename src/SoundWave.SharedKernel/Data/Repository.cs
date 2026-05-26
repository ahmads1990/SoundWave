using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using System.Linq.Expressions;

namespace SoundWave.SharedKernel.Data;

/// <summary>
/// Generic repository implementation for common data access operations.
/// </summary>
/// <typeparam name="TEntity">The type of the entity, inheriting from BaseEntity.</typeparam>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public class Repository<TEntity, TContext> : IRepository<TEntity>
    where TEntity : BaseEntity
    where TContext : DbContext
{
    protected readonly TContext _context;
    protected readonly DbSet<TEntity> _dbset;
    private static readonly string[] ImmutableFieldNames = { nameof(BaseEntity.Id), nameof(BaseEntity.CreatedDate), nameof(BaseEntity.CreatedBy), nameof(BaseEntity.UpdatedBy), nameof(BaseEntity.UpdatedDate) };

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public Repository(TContext dbContext)
    {
        _context = dbContext;
        _dbset = _context.Set<TEntity>();
    }

    #region Read Operations

    /// <inheritdoc/>
    public IQueryable<TEntity> GetAllWithDeleted()
    {
        return _dbset;
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> GetAll()
    {
        return _dbset.Where(e => !e.IsDeleted);
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> GetByID(Guid id)
    {
        return GetByCondition(x => x.Id == id);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetByID(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByCondition(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> GetByCondition(Expression<Func<TEntity, bool>> expression)
    {
        return GetAll().Where(expression);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await GetAll().Where(expression).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CheckExistsByID(Guid id, CancellationToken cancellationToken = default)
    {
        return await CheckExistsByCondition(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CheckExistsByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await GetAll().AnyAsync(expression, cancellationToken);
    }

    #endregion

    #region Write Operations

    /// <inheritdoc/>
    public async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _dbset.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    /// <inheritdoc/>
    public async Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbset.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc/>
    public void Update(TEntity entity)
    {
        _dbset.Update(entity);
    }

    /// <inheritdoc/>
    public void SaveInclude(TEntity entity, params string[] properties)
    {
        properties = properties.Except(ImmutableFieldNames).ToArray();

        var changeTrackerEntry = _context.ChangeTracker.Entries<TEntity>().FirstOrDefault(x => x.Entity.Id == entity.Id);
        if (changeTrackerEntry == null)
        {
            changeTrackerEntry = _dbset.Attach(entity);
        }

        var entityType = entity.GetType();

        foreach (var entryProperty in changeTrackerEntry.Properties)
        {
            if (properties.Contains(entryProperty.Metadata.Name))
            {
                var propInfo = entityType.GetProperty(entryProperty.Metadata.Name);
                if (propInfo != null)
                {
                    entryProperty.CurrentValue = propInfo.GetValue(entity);
                    entryProperty.IsModified = true;
                }
            }
        }
    }

    /// <inheritdoc/>
    public void SaveExclude(TEntity entity, params string[] properties)
    {
        properties = properties.Concat(ImmutableFieldNames).ToArray();

        var changeTrackerEntry = _context.ChangeTracker.Entries<TEntity>().FirstOrDefault(x => x.Entity.Id == entity.Id);
        if (changeTrackerEntry == null)
        {
            changeTrackerEntry = _dbset.Attach(entity);
        }

        var entityType = entity.GetType();

        foreach (var property in changeTrackerEntry.Properties)
        {
            if (!properties.Contains(property.Metadata.Name))
            {
                var propInfo = entityType.GetProperty(property.Metadata.Name);
                if (propInfo != null)
                {
                    property.CurrentValue = propInfo.GetValue(entity);
                    property.IsModified = true;
                }
            }
        }
    }

    #endregion

    #region Delete Operations

    /// <inheritdoc/>
    public void SoftDelete(TEntity entity)
    {
        entity.IsDeleted = true;
        SaveInclude(entity, nameof(entity.IsDeleted));
    }

    /// <inheritdoc/>
    public void Delete(TEntity entity)
    {
        _dbset.Remove(entity);
    }

    /// <inheritdoc/>
    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        _dbset.RemoveRange(entities);
    }

    #endregion

    #region Transaction Operations

    /// <inheritdoc/>
    public async Task<bool> SaveChanges(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    #endregion
}
