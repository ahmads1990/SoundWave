using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Data.IRepository;

/// <summary>
/// Write-side repository interface for the Playlist module.
/// Inject this in command handlers that need to mutate data.
/// </summary>
/// <typeparam name="TEntity">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
public interface IPlaylistRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
}
