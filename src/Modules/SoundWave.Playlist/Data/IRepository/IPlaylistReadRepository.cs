using System.Linq.Expressions;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Playlist.Data.IRepository;

/// <summary>
/// Read-only repository interface for the Playlist module.
/// Inject this in query handlers. No write methods — enforced at the interface level.
/// Backed by <see cref="PlaylistReadDbContext"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
public interface IPlaylistReadRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>Returns all active (non-deleted) entities with no EF tracking overhead.</summary>
    IQueryable<TEntity> GetAll();

    /// <summary>Returns a queryable filtered to the entity with the given id (no tracking).</summary>
    IQueryable<TEntity> GetByID(Guid id);

    /// <summary>Returns the entity with the given id, or null (no tracking).</summary>
    Task<TEntity?> GetByID(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a queryable filtered by the given predicate (no tracking).</summary>
    IQueryable<TEntity> GetByCondition(Expression<Func<TEntity, bool>> expression);

    /// <summary>Returns the first entity matching the predicate, or null (no tracking).</summary>
    Task<TEntity?> GetByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

    /// <summary>Returns true if an entity with the given id exists.</summary>
    Task<bool> CheckExistsByID(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns true if any entity matches the given predicate.</summary>
    Task<bool> CheckExistsByCondition(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
}
