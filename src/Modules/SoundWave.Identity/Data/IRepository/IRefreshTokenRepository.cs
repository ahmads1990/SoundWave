using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Dtos;

namespace SoundWave.Identity.Data.IRepository;

/// <summary>
/// Repository interface for User operations.
/// </summary>
internal interface IRefreshTokenRepository : IIdentityRepository<RefreshToken>
{
    /// <summary>
    /// Retrieves a valid, non-revoked refresh token for a user.
    /// </summary>
    Task<RefreshToken?> GetValidRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
