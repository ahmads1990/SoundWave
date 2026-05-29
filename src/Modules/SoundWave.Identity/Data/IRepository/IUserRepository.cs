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
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The User if found, otherwise null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user profile by the user's ID.
    /// </summary>
    Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves user login info (credentials + profile) by email for authentication.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UserLoginInfoDto if found, otherwise null.</returns>
    Task<UserLoginInfoDto?> GetUserLoginInfoByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves user info required for email verification operations.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UserVerificationInfoDto if found, otherwise null.</returns>
    Task<UserVerificationInfoDto?> GetUserVerificationInfoByEmailAsync(string email, CancellationToken cancellationToken = default);
}
