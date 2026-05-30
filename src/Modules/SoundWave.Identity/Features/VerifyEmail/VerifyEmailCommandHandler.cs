using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Features.VerifyEmail;

/// <summary>
/// Handles the verify email command.
/// </summary>
internal class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    ICachingService cachingService,
    ILogger<VerifyEmailCommandHandler> logger)
    : IRequestHandler<VerifyEmailCommand, IdentityResult<bool>>
{
    public async Task<IdentityResult<bool>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken = default)
    {
        var userInfo = await userRepository.GetUserVerificationInfoByEmailAsync(command.Email, cancellationToken);
        var validation = await Validate(command, userInfo);
        if (!validation.IsSuccess)
            return validation;

        var verification = await VerifyUserOtp(userInfo!.Id, command.Email, command.Otp, cancellationToken);
        if (!verification.IsSuccess)
            return verification;

        await MarkUserEmailVerified(userInfo.Id, cancellationToken);

        var cacheKey = Constants.Caching.UserEmailVerification + userInfo.Id.ToString();
        await cachingService.RemoveAsync(cacheKey, cancellationToken);

        logger.LogInformation("Email verified successfully for {UserId}", userInfo.Id);

        return IdentityResult<bool>.Success(true);
    }

    #region Private Methods

    private async Task<IdentityResult<bool>> Validate(VerifyEmailCommand command, UserVerificationInfoDto? userInfo)
    {
        if (userInfo == null)
        {
            logger.LogWarning("Email verification failed: user not found for {Email}", command.Email);
            return IdentityResult<bool>.Failure(IdentityError.UserNotFound, "User not found.");
        }

        if (userInfo.IsEmailVerified)
        {
            logger.LogInformation("Email verification skipped: {Email} is already verified", command.Email);
            return IdentityResult<bool>.Failure(IdentityError.EmailAlreadyVerified, "Email is already verified.");
        }

        return IdentityResult<bool>.Success(true);
    }

    private async Task<IdentityResult<bool>> VerifyUserOtp(Guid userId, string email, string otp, CancellationToken cancellationToken)
    {
        var cacheKey = Constants.Caching.UserEmailVerification + userId;
        var cachedOtp = await cachingService.GetAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedOtp) || cachedOtp != otp)
        {
            logger.LogWarning("Email verification failed: invalid or expired OTP for {Email}", email);
            return IdentityResult<bool>.Failure(IdentityError.InvalidToken, "Invalid or expired verification code.");
        }
        return IdentityResult<bool>.Success(true);
    }

    private async Task MarkUserEmailVerified(Guid userId, CancellationToken cancellationToken)
    {
        var user = new User { Id = userId, IsEmailVerified = true };
        userRepository.SaveInclude(user, nameof(User.IsEmailVerified));
        await userRepository.SaveChanges(cancellationToken);
    }

    #endregion
}
