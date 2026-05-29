using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Events.Notifications.UserRegistered;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Handles the registration of a new user within the identity module.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="userProfileRepository">The user profile repository.</param>
/// <param name="publisher">The MediatR publisher for dispatching domain events.</param>
/// <param name="logger">The logger.</param>
internal class RegisterCommandHandler(
    IUserRepository userRepository,
    IIdentityRepository<UserProfile> userProfileRepository,
    IPublisher publisher,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, IdentityResult<Guid>>
{
    /// <summary>
    /// Handles the registration process, including email validation, user creation, and profile setup.
    /// </summary>
    /// <param name="request">The registration command details.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An identity result containing the new user's unique identifier if successful.</returns>
    public async Task<IdentityResult<Guid>> Handle(RegisterCommand request, CancellationToken ct = default)
    {
        if (await userRepository.CheckIfEmailExistsAsync(request.Email, ct))
        {
            logger.LogWarning("Registration rejected — email {Email} already exists", request.Email);
            return IdentityResult<Guid>.Failure(IdentityError.EmailAlreadyExists);
        }

        var (user, profile) = CreateUserWithProfile(request);

        await userRepository.Add(user, ct);
        await userProfileRepository.Add(profile, ct);

        await userRepository.SaveChanges(ct);

        logger.LogInformation("User {UserId} registered successfully", user.Id);

        await publisher.Publish(new UserRegisteredNotification(user.Id, request.Email, request.DisplayName), ct);

        return IdentityResult<Guid>.Success(user.Id);
    }


    #region Private Methods

    /// <summary>
    /// Maps the registration command to both User and UserProfile entities.
    /// </summary>
    /// <param name="request">The source registration command.</param>
    /// <returns>A tuple containing the populated User and UserProfile entities.</returns>
    private static (User User, UserProfile Profile) CreateUserWithProfile(RegisterCommand request)
    {
        var userId = Guid.CreateVersion7();

        var user = request.Adapt<User>();
        user.Id = userId;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var profile = request.Adapt<UserProfile>();
        profile.Id = Guid.CreateVersion7();
        profile.UserId = userId;

        return (user, profile);
    }

    #endregion
}
