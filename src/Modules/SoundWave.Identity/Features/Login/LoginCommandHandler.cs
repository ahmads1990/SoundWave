using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Handles authenticating users, generating JWT access tokens and database-backed refresh tokens.
/// </summary>
/// <param name="userRepository">The user repository for user lookups.</param>
/// <param name="tokenHelper">Helper for creating JWT and refresh tokens.</param>
/// <param name="jwtOptions">Configuration settings for JWT lifetime and signing keys.</param>
/// <param name="logger">The logger instance.</param>
internal class LoginCommandHandler(
    IUserRepository userRepository,
    ITokenHelper tokenHelper,
    IOptions<JwtConfig> jwtOptions,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, BaseApiResponse<UserTokensDto>>
{
    #region Fields

    private readonly JwtConfig _jwtConfig = jwtOptions.Value;

    #endregion

    #region Public Methods

    /// <summary>
    /// Handles the authentication request. Verifies credentials and generates tokens if successful.
    /// </summary>
    /// <param name="command">The login command containing credentials.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>A base API response containing the access and refresh tokens if successful; otherwise, a failure response.</returns>
    public async Task<BaseApiResponse<UserTokensDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await userRepository.GetUserLoginInfoByEmailAsync(command.Email, cancellationToken);

        if (userInfo == null || !BCrypt.Net.BCrypt.Verify(command.Password, userInfo.PasswordHash))
        {
            logger.LogWarning("Authentication failed for email: {Email}", command.Email);
            return new FailureResponse<UserTokensDto>(ApiErrorCode.InvalidCredentials);
        }

        if (!userInfo.IsEmailVerified)
        {
            logger.LogWarning("Authentication blocked for email: {Email} — email is not verified.", command.Email);
            return new FailureResponse<UserTokensDto>(ApiErrorCode.EmailNotVerified)
            {
                Data = new UserTokensDto { UserId = userInfo.Id }
            };
        }

        var basicInfo = new UserBasicInfoDto
        {
            ID = userInfo.Id,
            Email = userInfo.Email,
            Role = userInfo.Role,
            Name = userInfo.Name,
            Username = userInfo.Username,
        };

        return await GenerateAuthTokensAsync(basicInfo, cancellationToken);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Generates access and refresh tokens for the authenticated user and persists the refresh token.
    /// </summary>
    /// <param name="userInfo">The basic profile information of the authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A base API response containing the generated tokens.</returns>
    private async Task<BaseApiResponse<UserTokensDto>> GenerateAuthTokensAsync(UserBasicInfoDto userInfo, CancellationToken cancellationToken)
    {
        var userClaims = new List<UserClaim> { new(CustomClaimTypes.Username, userInfo.Username) };
        var jwtToken = tokenHelper.GenerateJWT(
            new UserTokenBaseClaims(userInfo.ID, userInfo.Role, userInfo.Name, userInfo.Email), userClaims, 0);

        if (string.IsNullOrEmpty(jwtToken))
        {
            logger.LogError("Token generation failed for user: {UserId}", userInfo.ID);
            return new FailureResponse<UserTokensDto>(ApiErrorCode.InternalServerError, "Token generation failed.");
        }

        var newRefreshToken = await tokenHelper.GenerateAndSaveRefreshTokenAsync(userInfo.ID, null, cancellationToken);

        logger.LogInformation("User {UserId} logged in successfully", userInfo.ID);
        return new SuccessResponse<UserTokensDto>(new UserTokensDto { JwtToken = jwtToken, RefreshToken = newRefreshToken });
    }

    #endregion
}
