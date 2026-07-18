using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Handles authenticating users, generating JWT access tokens and database-backed refresh tokens.
/// </summary>
/// <param name="userRepository">The user repository for user lookups.</param>
/// <param name="tokenService">Service for managing JWT and refresh tokens.</param>
/// <param name="cachingService">The caching service for tracking failed login attempts.</param>
/// <param name="logger">The logger instance.</param>
internal class LoginCommandHandler(
    IIdentityRepository<User> userRepository,
    ITokenService tokenService,
    ICachingService cachingService,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<IdentityError, UserTokensDto>>
{
    /// <summary>
    /// Handles the authentication request. Verifies credentials and generates tokens if successful.
    /// </summary>
    /// <param name="command">The login command containing credentials.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>An identity result containing the access and refresh tokens if successful; otherwise, a failure response.</returns>
    public async Task<Result<IdentityError, UserTokensDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await GetUserLoginInfoAsync(command.Email, cancellationToken);
        var validation = Validate(command, userInfo);
        if (!validation.IsSuccess)
            return validation;

        if (!BCrypt.Net.BCrypt.Verify(command.Password, userInfo!.PasswordHash))
        {
            logger.LogWarning("Authentication failed for email: {Email}", command.Email);
            return await AddFailedLoginAttempt(userInfo, cancellationToken);
        }

        await ClearFailedLoginAttemptsAsync(userInfo.Id, true, cancellationToken);

        return await GenerateAuthTokensAsync(userInfo, cancellationToken);
    }

    #region Private Methods

    private Result<IdentityError, UserTokensDto> Validate(LoginCommand command, UserLoginInfoDto? userInfo)
    {
        if (userInfo == null)
        {
            logger.LogWarning("Authentication failed account not found for email: {Email}", command.Email);
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.InvalidCredentials);
        }

        if (userInfo.LockoutUntilUtc.HasValue && userInfo.LockoutUntilUtc > DateTime.UtcNow)
        {
            logger.LogWarning("Authentication blocked for locked account: {Email}", command.Email);
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.AccountLocked);
        }

        if (!userInfo.IsEmailVerified)
        {
            logger.LogWarning("Authentication blocked for email: {Email} — email is not verified.", command.Email);
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.EmailNotVerified, new UserTokensDto { UserId = userInfo.Id });
        }

        return Result<IdentityError, UserTokensDto>.Success(default!);
    }

    private Task<UserLoginInfoDto?> GetUserLoginInfoAsync(string email, CancellationToken cancellationToken)
    {
        return userRepository.GetAll()
            .Include(u => u.UserProfile)
            .Where(u => u.Email == email)
            .Select(u => new UserLoginInfoDto
            {
                Id = u.Id,
                Role = u.Role,
                PasswordHash = u.PasswordHash,
                LockoutUntilUtc = u.LockoutUntilUtc,
                IsEmailVerified = u.IsEmailVerified,
                Username = u.UserProfile != null ? u.UserProfile.DisplayName : string.Empty,
                Name = u.UserProfile != null ? $"{u.UserProfile.FirstName} {u.UserProfile.LastName}".Trim() : string.Empty,
                Email = u.Email
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Result<IdentityError, UserTokensDto>> GenerateAuthTokensAsync(UserLoginInfoDto userInfo, CancellationToken cancellationToken)
    {
        var tokens = await tokenService.GenerateUserTokensAsync(userInfo, null, cancellationToken);

        logger.LogInformation("User {UserId} logged in successfully", userInfo.Id);
        return Result<IdentityError, UserTokensDto>.Success(tokens);
    }

    private async Task<(int Count, bool IsFirst)> ReadAndIncrement(string key, CancellationToken cancellationToken = default)
    {
        var cachedFailedCount = await cachingService.GetAsync(key, cancellationToken);
        var isFirstAttempt = !int.TryParse(cachedFailedCount, out var currentFailedAttempts);
        return (currentFailedAttempts + 1, isFirstAttempt);
    }

    private async Task LockUser(Guid userId, DateTime lockoutTime, CancellationToken cancellationToken = default)
    {
        var user = new User { Id = userId, LockoutUntilUtc = lockoutTime };
        userRepository.SaveInclude(user, nameof(User.LockoutUntilUtc));
        await userRepository.SaveChanges(cancellationToken);
    }

    private async Task ClearFailedLoginAttemptsAsync(Guid userId, bool clearHardLock = true, CancellationToken cancellationToken = default)
    {
        var softLockKey = Constants.Caching.GetUserFailedLoginKey(userId);
        await cachingService.RemoveAsync(softLockKey, cancellationToken);

        if (clearHardLock)
        {
            var hardLockKey = Constants.Caching.GetUserHardFailedLoginKey(userId);
            await cachingService.RemoveAsync(hardLockKey, cancellationToken);
        }
    }

    /// <summary>
    /// Records a failed login attempt for the user. Handles both soft (temporary) and hard (permanent) 
    /// lockouts depending on the number of failures within the defined time windows.
    /// </summary>
    private async Task<Result<IdentityError, UserTokensDto>> AddFailedLoginAttempt(UserLoginInfoDto userInfo, CancellationToken cancellationToken)
    {
        var softLockKey = Constants.Caching.GetUserFailedLoginKey(userInfo.Id);
        var hardLockKey = Constants.Caching.GetUserHardFailedLoginKey(userInfo.Id);

        // Fetch current cache values and increment them in-memory
        var (failedCount, isFirstAttempt) = await ReadAndIncrement(softLockKey, cancellationToken);
        var (hardFailedCount, isFirstHardAttempt) = await ReadAndIncrement(hardLockKey, cancellationToken);

        // 1. Evaluate Hard Lockout 
        if (hardFailedCount >= Constants.MAX_HARD_FAILED_LOGIN_ATTEMPTS)
        {
            logger.LogWarning("User account hard locked due to {Attempts} failed login attempts: {UserId}", hardFailedCount, userInfo.Id);
            await LockUser(userInfo.Id, DateTime.UtcNow.AddYears(Constants.HARD_LOCKOUT_DURATION_YEARS), cancellationToken);

            // Clean up cache counters as the account is now completely locked
            await ClearFailedLoginAttemptsAsync(userInfo.Id, true, cancellationToken);

            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.AccountLocked);
        }

        // 2. Evaluate Soft Lockout
        if (failedCount >= Constants.MAX_FAILED_LOGIN_ATTEMPTS)
        {
            logger.LogWarning("User account temporarily locked due to {Attempts} failed login attempts: {UserId}", failedCount, userInfo.Id);
            await LockUser(userInfo.Id, DateTime.UtcNow.AddMinutes(Constants.SOFT_LOCKOUT_DURATION_MINUTES), cancellationToken);

            // Note: We only remove the soft lock key. The hard lock key survives so we can 
            // track repeated temporary lockouts over a longer window.
            await ClearFailedLoginAttemptsAsync(userInfo.Id, false, cancellationToken);

            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.AccountTemporarilyLocked);
        }

        // 3. Persist updated counters (No thresholds hit yet)
        // We only set the TTL on the very first attempt to ensure the time window doesn't slide/reset.
        var ttl = isFirstAttempt ? TimeSpan.FromMinutes(Constants.Caching.UserFailedLoginTtlMinutes) : (TimeSpan?)null;
        var ttlHard = isFirstHardAttempt ? TimeSpan.FromMinutes(Constants.Caching.UserHardFailedLoginTtlMinutes) : (TimeSpan?)null;
        
        await cachingService.AddAsync(softLockKey, failedCount.ToString(), ttl, cancellationToken);
        await cachingService.AddAsync(hardLockKey, hardFailedCount.ToString(), ttlHard, cancellationToken);

        return Result<IdentityError, UserTokensDto>.Failure(IdentityError.InvalidCredentials);
    }

    #endregion
}
