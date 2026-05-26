using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Data.IRepository;

/// <summary>
/// Identity module specific repository interface.
/// </summary>
public interface IIdentityRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
}
