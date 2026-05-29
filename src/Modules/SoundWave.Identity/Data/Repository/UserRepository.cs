using Mapster;
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
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckIfEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await CheckExistsByCondition(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await GetByCondition(u => u.Email == email).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<UserVerificationInfoDto?> GetUserVerificationInfoByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await GetByCondition(u => u.Email == email)
            .Select(u => new UserVerificationInfoDto
            {
                Id = u.Id,
                Email = u.Email,
                IsEmailVerified = u.IsEmailVerified,
                FirstName = u.UserProfile != null ? u.UserProfile.FirstName : string.Empty,
                LastName = u.UserProfile != null ? u.UserProfile.LastName : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

