using SoundWave.Identity.Data.Entites;

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
}
