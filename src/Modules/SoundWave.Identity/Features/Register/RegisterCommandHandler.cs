using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Events.Notifications.UserRegistered;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Handles the registration of a new user within the identity module.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="userProfileRepository">The user profile repository.</param>
/// <param name="publisher">The MediatR publisher for dispatching domain events.</param>
/// <param name="logger">The logger.</param>
internal class RegisterCommandHandler(
    IIdentityRepository<User> userRepository, 
    IIdentityRepository<UserProfile> userProfileRepository, 
    IPublisher publisher,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, BaseApiResponse<Guid>>
{
    /// <summary>
    /// Handles the registration process, including email validation, user creation, and profile setup.
    /// </summary>
    /// <param name="request">The registration command details.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A base API response containing the new user's unique identifier if successful.</returns>
    public async Task<BaseApiResponse<Guid>> Handle(RegisterCommand request, CancellationToken ct = default)
    {
        if (await CheckIfEmailExistsAsync(request.Email, ct))
        {
            logger.LogWarning("Registration rejected — email {Email} already exists", request.Email);
            return new FailureResponse<Guid>(ApiErrorCode.EmailAlreadyExists);
        }

        var userId = Guid.CreateVersion7();
        var user = CreateUser(request, userId);
        var profile = CreateUserProfile(request, userId);

        await userRepository.Add(user, ct);
        await userProfileRepository.Add(profile, ct);
        
        await userRepository.SaveChanges(ct);

        logger.LogInformation("User {UserId} registered successfully", userId);

        await publisher.Publish(new UserRegisteredNotification(userId, request.Email, request.DisplayName), ct);

        logger.LogDebug("UserRegisteredNotification published for {UserId}", userId);

        return new SuccessResponse<Guid>(userId);
    }

    #region Private Methods

    /// <summary>
    /// Checks the database for any existing user with the provided email address.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="ct">Cancellation token for the database query.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean result indicating existence.</returns>
    private async Task<bool> CheckIfEmailExistsAsync(string email, CancellationToken ct)
    {
        return await userRepository.CheckExistsByCondition(u => u.Email == email, ct);
    }

    /// <summary>
    /// Maps the registration command to a new User entity and hashes the password.
    /// </summary>
    /// <param name="request">The source registration command.</param>
    /// <param name="userId">The pre-generated unique identifier for the user.</param>
    /// <returns>A populated User entity.</returns>
    private static User CreateUser(RegisterCommand request, Guid userId)
    {
        var user = request.Adapt<User>();
        user.Id = userId;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        return user;
    }

    /// <summary>
    /// Maps the registration command to a new UserProfile entity.
    /// </summary>
    /// <param name="request">The source registration command.</param>
    /// <param name="userId">The unique identifier of the associated user.</param>
    /// <returns>A populated UserProfile entity.</returns>
    private static UserProfile CreateUserProfile(RegisterCommand request, Guid userId)
    {
        var profile = request.Adapt<UserProfile>();
        profile.Id = Guid.CreateVersion7();
        profile.UserId = userId;
        return profile;
    }

    #endregion
}
