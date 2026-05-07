using System.Linq.Expressions;

namespace SoundWave.SharedKernel.Interfaces;

/// <summary>
/// Defines a generic repository for common data access operations.
/// </summary>
/// <typeparam name="Entity">The type of the entity.</typeparam>
public interface IRepository<Entity>
{
    #region Read Operations

    /// <summary>
    /// Retrieves all entities, including those marked as soft-deleted.
    /// </summary>
    IQueryable<Entity> GetAllWithDeleted();

    /// <summary>
    /// Retrieves all active (non-deleted) entities.
    /// </summary>
    IQueryable<Entity> GetAll();

    /// <summary>
    /// Retrieves an entity by its unique Guid identifier.
    /// </summary>
    IQueryable<Entity> GetByID(Guid id);

    /// <summary>
    /// Retrieves an entity by its unique Guid identifier asynchronously.
    /// </summary>
    Task<Entity?> GetByID(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves entities matching the specified condition.
    /// </summary>
    IQueryable<Entity> GetByCondition(Expression<Func<Entity, bool>> expression);

    /// <summary>
    /// Retrieves the first entity matching the specified condition asynchronously.
    /// </summary>
    Task<Entity?> GetByCondition(Expression<Func<Entity, bool>> expression, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity exists with the specified Guid identifier.
    /// </summary>
    Task<bool> CheckExistsByID(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities match the specified condition.
    /// </summary>
    Task<bool> CheckExistsByCondition(Expression<Func<Entity, bool>> expression, CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Adds a new entity asynchronously.
    /// </summary>
    Task<Entity> Add(Entity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a collection of new entities asynchronously.
    /// </summary>
    Task AddRange(IEnumerable<Entity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    void Update(Entity entity);

    /// <summary>
    /// Saves an entity by only updating the specifically included properties.
    /// </summary>
    void SaveInclude(Entity entity, params string[] properties);

    /// <summary>
    /// Saves an entity by updating all properties except the excluded ones.
    /// </summary>
    void SaveExclude(Entity entity, params string[] properties);

    #endregion

    #region Delete Operations

    /// <summary>
    /// Performs a soft delete by marking the entity as deleted.
    /// </summary>
    void SoftDelete(Entity entity);

    /// <summary>
    /// Hard deletes an entity from the database.
    /// </summary>
    void Delete(Entity entity);

    /// <summary>
    /// Hard deletes a collection of entities from the database.
    /// </summary>
    void DeleteRange(IEnumerable<Entity> entities);

    #endregion

    #region Transaction Operations

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>True if one or more state entries were written to the database, false otherwise.</returns>
    Task<bool> SaveChanges(CancellationToken cancellationToken = default);

    #endregion
}
