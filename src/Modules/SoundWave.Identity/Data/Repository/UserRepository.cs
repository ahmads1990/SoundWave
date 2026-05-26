using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;

namespace SoundWave.Identity.Data.Repository;

/// <summary>
/// Repository implementation for User operations.
/// </summary>
internal class UserRepository : IdentityRepository<User>, IUserRepository
{
    public UserRepository(IdentityDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> CheckIfEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await CheckExistsByCondition(u => u.Email == email, cancellationToken);
    }
}
