using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;

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

    public async Task<UserLoginInfoDto?> GetUserLoginInfoByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await GetByCondition(u => u.Email == email)
            .Select(u => new UserLoginInfoDto
            {
                Id = u.Id,
                PasswordHash = u.PasswordHash,
                IsEmailVerified = u.IsEmailVerified,
                Email = u.Email,
                Role = u.Role,
                Name = u.UserProfile != null ? (u.UserProfile.FirstName + " " + u.UserProfile.LastName).Trim() : string.Empty,
                Username = u.UserProfile != null ? u.UserProfile.DisplayName : string.Empty,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

