using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Features.PasswordReset;

/// <summary>
/// Handles validating the OTP token and setting the new password.
/// </summary>
internal class ResetPasswordCommandHandler(
    IIdentityRepository<User> userRepository,
    ICachingService cachingService,
    ILogger<ResetPasswordCommandHandler> logger)
    : IRequestHandler<ResetPasswordCommand, IdentityResult<bool>>
{
    public async Task<IdentityResult<bool>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var validation = await Validate(command, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var user = await GetUserByEmailAsync(command.Email, cancellationToken);

        await UpdatePasswordAndUnlockAsync(user, command.NewPassword, cancellationToken);
        await ClearCacheAsync(user.Id, cancellationToken);

        logger.LogInformation("Password reset successfully for user: {UserId}", user.Id);

        return IdentityResult<bool>.Success(true);
    }

    #region Private Methods

    private async Task<IdentityResult<bool>> Validate(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAll().FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Password reset attempted for non-existent email: {Email}", command.Email);
            return IdentityResult<bool>.Failure(IdentityError.UserNotFound);
        }

        var cacheKey = Constants.Caching.GetUserPasswordResetKey(user.Id);
        var cachedToken = await cachingService.GetAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedToken) || cachedToken != command.Token)
        {
            logger.LogWarning("Invalid or expired password reset token provided for user: {UserId}", user.Id);
            return IdentityResult<bool>.Failure(IdentityError.InvalidToken, "The password reset token is invalid or has expired.");
        }

        return IdentityResult<bool>.Success(true);
    }

    private Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return userRepository.GetAll().FirstAsync(u => u.Email == email, cancellationToken);
    }

    private async Task UpdatePasswordAndUnlockAsync(User user, string newPassword, CancellationToken cancellationToken)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.LockoutUntilUtc = null;

        userRepository.SaveInclude(user, nameof(User.PasswordHash), nameof(User.LockoutUntilUtc));
        await userRepository.SaveChanges(cancellationToken);
    }

    private async Task ClearCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        var resetTokenKey = Constants.Caching.GetUserPasswordResetKey(userId);
        await cachingService.RemoveAsync(resetTokenKey, cancellationToken);

        var softLockKey = Constants.Caching.GetUserFailedLoginKey(userId);
        var hardLockKey = Constants.Caching.GetUserHardFailedLoginKey(userId);
        await cachingService.RemoveAsync(softLockKey, cancellationToken);
        await cachingService.RemoveAsync(hardLockKey, cancellationToken);
    }

    #endregion
}
