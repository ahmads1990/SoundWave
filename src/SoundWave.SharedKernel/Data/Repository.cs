using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using System.Linq.Expressions;

namespace SoundWave.SharedKernel.Data;

public class Repository<TEntity, TContext> : IRepository<TEntity> 
    where TEntity : BaseEntity 
    where TContext : DbContext
{
    protected readonly TContext _context;
    protected readonly DbSet<TEntity> _dbset;
    private static readonly string[] ImmutableFieldNames = { nameof(BaseEntity.Id), nameof(BaseEntity.CreatedDate), nameof(BaseEntity.CreatedBy), nameof(BaseEntity.UpdatedBy), nameof(BaseEntity.UpdatedDate) };

    public Repository(TContext dbContext)
    {
        _context = dbContext;
        _dbset = _context.Set<TEntity>();
    }

    public IQueryable<TEntity> GetAllWithDeleted()
    {
        return _dbset;
    }

    public IQueryable<TEntity> GetAll()
    {
        return _dbset.Where(e => !e.IsDeleted);
    }

    public IQueryable<TEntity> GetByCondition(Expression<Func<TEntity, bool>> expression)
    {
        return GetAll().Where(expression);
    }

    public async Task<TEntity?> GetByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await GetAll().Where(expression).FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<TEntity> GetByID(int id)
    {
        // NOTE: The ID might be a Guid according to ROADMAP. If IRepository defines int id, we must match it or change IRepository.
        throw new NotImplementedException("Use GetById(Guid) for Guid based entities.");
    }

    public async Task<TEntity?> GetByID(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use GetById(Guid) for Guid based entities.");
    }
    
    public IQueryable<TEntity> GetByID(Guid id)
    {
        return GetByCondition(x => x.Id == id);
    }

    public async Task<TEntity?> GetByID(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByCondition(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CheckExistsByID(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use CheckExistsByID(Guid).");
    }

    public async Task<bool> CheckExistsByID(Guid id, CancellationToken cancellationToken = default)
    {
        return await CheckExistsByCondition(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> CheckExistsByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await GetAll().AnyAsync(expression, cancellationToken);
    }

    public async Task<TEntity> Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _dbset.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public async Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbset.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbset.Update(entity);
    }

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

    public void SoftDelete(TEntity entity)
    {
        entity.IsDeleted = true;
        SaveInclude(entity, nameof(entity.IsDeleted));
    }

    public void Delete(TEntity entity)
    {
        _dbset.Remove(entity);
    }

    public void DeleteRange(IEnumerable<TEntity> entity)
    {
        _dbset.RemoveRange(entity);
    }

    public async Task<bool> SaveChanges(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }
}
