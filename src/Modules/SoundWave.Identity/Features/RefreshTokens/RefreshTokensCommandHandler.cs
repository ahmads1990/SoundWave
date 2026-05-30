using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SoundWave.Identity.Features.RefreshTokens;

/// <summary>
/// Handles refreshing session tokens by validating the refresh token and generating a new token pair.
/// </summary>
internal class RefreshTokensCommandHandler(
    IRefreshTokenRepository refreshTokenRepo,
    IUserRepository userRepository,
    ITokenHelper tokenHelper,
    ILogger<RefreshTokensCommandHandler> logger)
    : IRequestHandler<RefreshTokensCommand, IdentityResult<UserTokensDto>>
{
    /// <summary>
    /// Handles the refresh token request.
    /// </summary>
    public async Task<IdentityResult<UserTokensDto>> Handle(RefreshTokensCommand command, CancellationToken cancellationToken)
    {
        var storedRefreshToken = await refreshTokenRepo.GetValidRefreshTokenAsync(command.UserId, cancellationToken);
        var validation = Validate(command, storedRefreshToken);
        if (!validation.IsSuccess)
            return validation;

        var userInfo = await userRepository.GetUserLoginInfoByIdAsync(command.UserId, cancellationToken);
        if (userInfo == null || userInfo.IsLocked)
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InvalidCredentials, "Invalid or locked user account.");

        var userClaims = new List<UserClaim> { new(CustomClaimTypes.Username, userInfo.Username) };
        var jwtToken = tokenHelper.GenerateJWT(
            new UserTokenBaseClaims(userInfo.Id, userInfo.Role, userInfo.Name, userInfo.Email), userClaims, 0);

        if (string.IsNullOrEmpty(jwtToken))
        {
            logger.LogError("Token generation failed during refresh for user: {UserId}", command.UserId);
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InternalError, "Token generation failed.");
        }

        var newRefreshToken = await tokenHelper.GenerateAndSaveRefreshTokenAsync(command.UserId, storedRefreshToken!.Id, cancellationToken);

        logger.LogInformation("Refresh token rotated successfully for user {UserId}", command.UserId);

        return IdentityResult<UserTokensDto>.Success(new UserTokensDto { JwtToken = jwtToken, RefreshToken = newRefreshToken });
    }

    private IdentityResult<UserTokensDto> Validate(RefreshTokensCommand command, Data.Entites.RefreshToken? storedRefreshToken)
    {
        if (storedRefreshToken is null)
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InvalidToken, "Refresh token not found or revoked.");

        if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InvalidToken, "Refresh token expired.");

        bool isValid = BCrypt.Net.BCrypt.Verify(command.RefreshToken, storedRefreshToken.TokenHash);
        if (!isValid)
            return IdentityResult<UserTokensDto>.Failure(IdentityError.InvalidToken, "Invalid refresh token.");

        return IdentityResult<UserTokensDto>.Success(default!);
    }
}
