namespace SoundWave.SharedKernel.Interfaces;

/// <summary>
/// Provides access to the current user's identity information.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's identifier.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
