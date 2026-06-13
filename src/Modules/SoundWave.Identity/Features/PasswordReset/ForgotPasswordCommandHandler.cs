using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Interfaces;

using SoundWave.Identity.Events.Notifications.PasswordResetRequested;

namespace SoundWave.Identity.Features.PasswordReset;

/// <summary>
/// Handles generating a password reset OTP and caching it.
/// </summary>
internal class ForgotPasswordCommandHandler(
    IIdentityRepository<User> userRepository,
    IOtpService otpService,
    ICachingService cachingService,
    IPublisher publisher,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, IdentityResult<bool>>
{
    public async Task<IdentityResult<bool>> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var validation = await Validate(command, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var user = await GetUserByEmailAsync(command.Email, cancellationToken);
        var otp = await GenerateAndCacheOtpAsync(user.Id, cancellationToken);

        await publisher.Publish(new PasswordResetRequestedNotification(user.Id, user.Email, otp), cancellationToken);

        logger.LogInformation("Password reset requested for user {UserId}", user.Id);

        return IdentityResult<bool>.Success(true);
    }

    #region Private Methods

    private async Task<IdentityResult<bool>> Validate(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var userExists = await userRepository.GetAll().AnyAsync(u => u.Email == command.Email, cancellationToken);
        if (!userExists)
        {
            logger.LogWarning("Password reset requested for non-existent email: {Email}", command.Email);
            return IdentityResult<bool>.Failure(IdentityError.UserNotFound);
        }

        return IdentityResult<bool>.Success(true);
    }

    private Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return userRepository.GetAll().FirstAsync(u => u.Email == email, cancellationToken);
    }

    private async Task<string> GenerateAndCacheOtpAsync(Guid userId, CancellationToken cancellationToken)
    {
        var otp = otpService.GenerateOtp();
        var cacheKey = Constants.Caching.GetUserPasswordResetKey(userId);
        var ttl = TimeSpan.FromMinutes(Constants.Caching.UserPasswordResetTtlMinutes);

        await cachingService.AddAsync(cacheKey, otp, ttl, cancellationToken);
        
        return otp;
    }

    #endregion
}
