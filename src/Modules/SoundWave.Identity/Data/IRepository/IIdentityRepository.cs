using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Data.IRepository;

/// <summary>
/// Identity module specific repository interface.
/// </summary>
/// <typeparam name="TEntity">The type of the entity, which must derive from BaseEntity.</typeparam>
public interface IIdentityRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
}
