using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.VerifyEmail;

/// <summary>
/// Handles the verify email command.
/// </summary>
internal class VerifyEmailCommandHandler(
    IIdentityRepository<User> userRepository,
    ICachingService cachingService,
    ILogger<VerifyEmailCommandHandler> logger)
    : IRequestHandler<VerifyEmailCommand, Result<IdentityError, bool>>
{
    public async Task<Result<IdentityError, bool>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken = default)
    {
        var userInfo = await GetUserVerificationInfoAsync(command.Email, cancellationToken);

        var validation = await Validate(command, userInfo);
        if (!validation.IsSuccess)
            return validation;

        var verification = await VerifyUserOtp(userInfo!.Id, command.Email, command.Otp, cancellationToken);
        if (!verification.IsSuccess)
            return verification;

        await MarkUserEmailVerified(userInfo.Id, cancellationToken);

        var cacheKey = Constants.Caching.GetUserEmailVerificationKey(userInfo.Id);
        await cachingService.RemoveAsync(cacheKey, cancellationToken);

        logger.LogInformation("Email verified successfully for {UserId}", userInfo.Id);

        return Result<IdentityError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<IdentityError, bool>> Validate(VerifyEmailCommand command, UserVerificationInfoDto? userInfo)
    {
        if (userInfo == null)
        {
            logger.LogWarning("Email verification failed: user not found for {Email}", command.Email);
            return Result<IdentityError, bool>.Failure(IdentityError.UserNotFound, "User not found.");
        }

        if (userInfo.IsEmailVerified)
        {
            logger.LogInformation("Email verification skipped: {Email} is already verified", command.Email);
            return Result<IdentityError, bool>.Failure(IdentityError.EmailAlreadyVerified, "Email is already verified.");
        }

        return Result<IdentityError, bool>.Success(true);
    }

    private Task<UserVerificationInfoDto?> GetUserVerificationInfoAsync(string email, CancellationToken cancellationToken)
    {
        return userRepository.GetAll()
            .Include(u => u.UserProfile)
            .Where(u => u.Email == email)
            .Select(u => new UserVerificationInfoDto
            {
                Id = u.Id,
                Email = u.Email,
                IsEmailVerified = u.IsEmailVerified,
                FirstName = u.UserProfile != null ? u.UserProfile.FirstName : string.Empty,
                LastName = u.UserProfile != null ? u.UserProfile.LastName : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Result<IdentityError, bool>> VerifyUserOtp(Guid userId, string email, string otp, CancellationToken cancellationToken)
    {
        var cacheKey = Constants.Caching.GetUserEmailVerificationKey(userId);
        var cachedOtp = await cachingService.GetAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedOtp) || cachedOtp != otp)
        {
            logger.LogWarning("Email verification failed: invalid or expired OTP for {Email}", email);
            return Result<IdentityError, bool>.Failure(IdentityError.InvalidToken, "Invalid or expired verification code.");
        }
        return Result<IdentityError, bool>.Success(true);
    }

    private async Task MarkUserEmailVerified(Guid userId, CancellationToken cancellationToken)
    {
        var user = new User { Id = userId, IsEmailVerified = true };
        userRepository.SaveInclude(user, nameof(User.IsEmailVerified));
        await userRepository.SaveChanges(cancellationToken);
    }

    #endregion
}
