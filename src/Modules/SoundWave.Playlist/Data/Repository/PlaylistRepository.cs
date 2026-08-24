using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Playlist.Data.Repository;

/// <summary>
/// Write-side repository implementation for the Playlist module.
/// Backed by <see cref="PlaylistDbContext"/> — full EF tracking, supports all CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
internal class PlaylistRepository<TEntity> : Repository<TEntity, PlaylistDbContext>, IPlaylistRepository<TEntity>
    where TEntity : BaseEntity
{
    public PlaylistRepository(PlaylistDbContext dbContext) : base(dbContext)
    {
    }
}
