using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Events.Notifications.UserRegistered;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Handles the registration of a new user within the identity module.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="userProfileRepository">The user profile repository.</param>
/// <param name="cachingService">The caching service for storing verification OTPs.</param>
/// <param name="tokenHelper">The token helper for generating OTPs.</param>
/// <param name="publisher">The MediatR publisher for dispatching domain events.</param>
/// <param name="logger">The logger.</param>
internal class RegisterCommandHandler(
    IUserRepository userRepository,
    IIdentityRepository<UserProfile> userProfileRepository,
    ICachingService cachingService,
    ITokenHelper tokenHelper,
    IPublisher publisher,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, IdentityResult<Guid>>
{
    /// <summary>
    /// Handles the registration process, including email validation, user creation, and profile setup.
    /// </summary>
    /// <param name="request">The registration command details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An identity result containing the new user's unique identifier if successful.</returns>
    public async Task<IdentityResult<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        var validation = await Validate(request, cancellationToken);
        if (!validation.IsSuccess)
            return validation;

        var (user, profile) = CreateUserWithProfile(request);
        await SaveUserWithProfile(user, profile, cancellationToken);

        var otp = await GenerateEmailVerificationOtp(user.Id);
        await publisher.Publish(new UserRegisteredNotification(user.Id, request.Email, request.DisplayName, otp), cancellationToken);

        return IdentityResult<Guid>.Success(user.Id);
    }

    #region Private Methods

    /// <summary>
    /// Validates the registration command, checking if the email address is already registered.
    /// </summary>
    /// <param name="request">The registration command containing the user data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A successful IdentityResult, or a failure result if the email already exists.</returns>
    private async Task<IdentityResult<Guid>> Validate(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        if (await userRepository.CheckIfEmailExistsAsync(request.Email, cancellationToken))
        {
            logger.LogWarning("Registration rejected — email {Email} already exists", request.Email);
            return IdentityResult<Guid>.Failure(IdentityError.EmailAlreadyExists);
        }
        return IdentityResult<Guid>.Success(Guid.Empty);
    }

    /// <summary>
    /// Maps the registration command to both User and UserProfile entities.
    /// </summary>
    /// <param name="request">The source registration command.</param>
    /// <returns>A tuple containing the populated User and UserProfile entities.</returns>
    private (User User, UserProfile Profile) CreateUserWithProfile(RegisterCommand request)
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

    /// <summary>
    /// Saves the User and UserProfile entities to the database in a single unit of work.
    /// </summary>
    /// <param name="user">The User entity to add.</param>
    /// <param name="profile">The UserProfile entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    private async Task SaveUserWithProfile(User user, UserProfile profile, CancellationToken cancellationToken = default)
    {
        await userRepository.Add(user, cancellationToken);
        await userProfileRepository.Add(profile, cancellationToken);
        await userRepository.SaveChanges(cancellationToken);

        logger.LogInformation("User {UserId} registered successfully", user.Id);
    }

    /// <summary>
    /// Generates a numeric OTP for email verification, stores it in the cache with the configured TTL constant, and returns it.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The generated OTP string.</returns>
    private async Task<string> GenerateEmailVerificationOtp(Guid userId)
    {
        var otp = tokenHelper.GenerateOTP();
        var cacheKey = Constants.Caching.UserEmailVerification + userId.ToString();
        var ttl = TimeSpan.FromMinutes(Constants.Caching.UserEmailVerificationTtlMinutes);
        await cachingService.AddAsync(cacheKey, otp, ttl);
        return otp;
    }

    #endregion
}
