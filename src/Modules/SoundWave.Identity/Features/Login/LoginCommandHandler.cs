using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Handles authenticating users, generating JWT access tokens and database-backed refresh tokens.
/// </summary>
/// <param name="userRepository">The user repository for user lookups.</param>
/// <param name="tokenHelper">Helper for creating JWT and refresh tokens.</param>
/// <param name="cachingService">The caching service for tracking failed login attempts.</param>
/// <param name="logger">The logger instance.</param>
internal class LoginCommandHandler(
    IUserRepository userRepository,
    ITokenHelper tokenHelper,
    ICachingService cachingService,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, IdentityResult<UserTokensDto>>
{
    /// <summary>
    /// Handles the authentication request. Verifies credentials and generates tokens if successful.
    /// </summary>
    /// <param name="command">The login command containing credentials.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>An identity result containing the access and refresh tokens if successful; otherwise, a failure response.</returns>
    public async Task<IdentityResult<UserTokensDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await userRepository.GetUserLoginInfoByEmailAsync(command.Email, cancellationToken);
        var validation = Validate(command, userInfo);
        if (!validation.IsSuccess)
            return validation;

        if (!BCrypt.Net.BCrypt.Verify(command.Password, userInfo!.PasswordHash))
        {
            logger.LogWarning("Authentication failed for email: {Email}", command.Email);
            return await AddFailedLoginAttempt(userInfo, cancellationToken);
        }

        return await GenerateAuthTokensAsync(userInfo, cancellationToken);
    }

    #region Private Methods


    private IdentityResult<UserTokensDto> Validate(LoginCommand command, UserLoginInfoDto? userInfo)
    {
        if (userInfo == null)
        {
            logger.LogWarning("Authentication failed account not found for email: {Email}", command.Email);
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InvalidCredentials);
        }

        if (userInfo.IsLocked)
        {
            logger.LogWarning("Authentication blocked for locked account: {Email}", command.Email);
            return IdentityResult<UserTokensDto>.Failure(IdentityError.AccountLocked);
        }

        if (!userInfo.IsEmailVerified)
        {
            logger.LogWarning("Authentication blocked for email: {Email} — email is not verified.", command.Email);
            return IdentityResult<UserTokensDto>.Failure(IdentityError.EmailNotVerified, new UserTokensDto { UserId = userInfo.Id });
        }

        return IdentityResult<UserTokensDto>.Success(default!);
    }

    private async Task<IdentityResult<UserTokensDto>> AddFailedLoginAttempt(UserLoginInfoDto userInfo, CancellationToken cancellationToken)
    {
        var key = $"{Constants.Caching.UserFailedLogin}{userInfo.Id}";

        var cached = await cachingService.GetAsync(key, cancellationToken);
        var isFirstAttempt = !int.TryParse(cached, out var currentFailedAttempts);

        var newFailedAttempts = currentFailedAttempts + 1;

        if (newFailedAttempts >= Constants.MAX_FAILED_LOGIN_ATTEMPTS)
        {
            var user = new User { Id = userInfo.Id, IsLocked = true };
            userRepository.SaveInclude(user, nameof(User.IsLocked));
            await userRepository.SaveChanges(cancellationToken);
            await cachingService.RemoveAsync(key, cancellationToken);

            logger.LogWarning("User account locked due to too many failed login attempts: {UserId}", userInfo.Id);
            return IdentityResult<UserTokensDto>.Failure(IdentityError.AccountLocked);
        }

        // Only set TTL on the first attempt (or if cache data was corrupt) so the window doesn't reset on every failure.
        var ttl = isFirstAttempt ? TimeSpan.FromMinutes(Constants.Caching.UserFailedLoginTtlMinutes) : (TimeSpan?)null;
        await cachingService.AddAsync(key, newFailedAttempts.ToString(), ttl, cancellationToken);

        return IdentityResult<UserTokensDto>.Failure(IdentityError.InvalidCredentials);
    }

    /// <summary>
    /// Generates access and refresh tokens for the authenticated user and persists the refresh token.
    /// </summary>
    /// <param name="userInfo">The basic profile information of the authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An identity result containing the generated tokens.</returns>
    private async Task<IdentityResult<UserTokensDto>> GenerateAuthTokensAsync(UserLoginInfoDto userInfo, CancellationToken cancellationToken)
    {
        var userClaims = new List<UserClaim> { new(CustomClaimTypes.Username, userInfo.Username) };
        var jwtToken = tokenHelper.GenerateJWT(
            new UserTokenBaseClaims(userInfo.Id, userInfo.Role, userInfo.Name, userInfo.Email), userClaims, 0);

        if (string.IsNullOrEmpty(jwtToken))
        {
            logger.LogError("Token generation failed for user: {UserId}", userInfo.Id);
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InternalError, "Token generation failed.");
        }

        var newRefreshToken = await tokenHelper.GenerateAndSaveRefreshTokenAsync(userInfo.Id, null, cancellationToken);

        logger.LogInformation("User {UserId} logged in successfully", userInfo.Id);
        return IdentityResult<UserTokensDto>.Success(new UserTokensDto { JwtToken = jwtToken, RefreshToken = newRefreshToken });
    }

    #endregion
}
