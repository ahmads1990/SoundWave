using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Events.Notifications.VerificationEmailRequested;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.ResendVerificationEmail;

/// <summary>
/// Handles the resend verification email command.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="cachingService">The caching service for storing the new OTP.</param>
/// <param name="otpService">The OTP service for generating OTPs.</param>
/// <param name="publisher">The MediatR publisher for dispatching domain events.</param>
/// <param name="logger">The logger.</param>
internal class ResendVerificationEmailCommandHandler(
    IIdentityRepository<User> userRepository,
    ICachingService cachingService,
    IOtpService otpService,
    IPublisher publisher,
    ILogger<ResendVerificationEmailCommandHandler> logger)
    : IRequestHandler<ResendVerificationEmailCommand, Result<IdentityError, bool>>
{
    /// <summary>
    /// Handles the command by validating the request, generating a new OTP, and triggering the email.
    /// </summary>
    public async Task<Result<IdentityError, bool>> Handle(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default)
    {
        var userInfo = await GetUserVerificationInfoAsync(command.Email, cancellationToken);

        var validation = await Validate(command, userInfo);
        if (!validation.IsSuccess)
            return validation.ToFailure<bool>();

        var otp = await GenerateOTP(userInfo!.Id, cancellationToken);
        await PublishNotificationAsync(userInfo, otp, cancellationToken);

        return Result<IdentityError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<IdentityError, UserVerificationInfoDto>> Validate(ResendVerificationEmailCommand command, UserVerificationInfoDto? userInfo)
    {
        if (userInfo == null)
        {
            logger.LogWarning("Resend verification email failed: user not found for {Email}", command.Email);
            return Result<IdentityError, UserVerificationInfoDto>.Failure(IdentityError.UserNotFound, "User not found.");
        }

        if (userInfo.IsEmailVerified)
        {
            logger.LogInformation("Resend verification email requested for {Email} but already verified", command.Email);
            return Result<IdentityError, UserVerificationInfoDto>.Failure(IdentityError.EmailAlreadyVerified, "Email is already verified.");
        }
        return Result<IdentityError, UserVerificationInfoDto>.Success(userInfo);
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

    private async Task<string> GenerateOTP(Guid userId, CancellationToken cancellationToken)
    {
        var otp = otpService.GenerateOtp();
        var cacheKey = Constants.Caching.GetUserEmailVerificationKey(userId);
        var ttl = TimeSpan.FromMinutes(Constants.Caching.UserEmailVerificationTtlMinutes);

        await cachingService.AddAsync(cacheKey, otp, ttl, cancellationToken);
        return otp;
    }

    private async Task PublishNotificationAsync(UserVerificationInfoDto userInfo, string otp, CancellationToken cancellationToken)
    {
        var fullName = !string.IsNullOrWhiteSpace(userInfo.FirstName)
            ? $"{userInfo.FirstName} {userInfo.LastName}".Trim()
            : userInfo.Email;

        await publisher.Publish(new VerificationEmailRequestedNotification(userInfo.Id, userInfo.Email, fullName, otp), cancellationToken);

        logger.LogInformation("New OTP generated and verification email requested for {UserId}", userInfo.Id);
    }

    #endregion
}
