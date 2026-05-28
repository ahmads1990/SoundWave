using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Dtos;

namespace SoundWave.Identity.Data.IRepository;

/// <summary>
/// Repository interface for User operations.
/// </summary>
internal interface IUserRepository : IIdentityRepository<User>
{
    /// <summary>
    /// Checks if a user with the specified email already exists.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if email exists, otherwise false.</returns>
    Task<bool> CheckIfEmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves user login info (credentials + profile) by email for authentication.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UserLoginInfoDto if found, otherwise null.</returns>
    Task<UserLoginInfoDto?> GetUserLoginInfoByEmailAsync(string email, CancellationToken cancellationToken = default);
}

