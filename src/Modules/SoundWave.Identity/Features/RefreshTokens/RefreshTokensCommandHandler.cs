using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.RefreshTokens;

/// <summary>
/// Handles refreshing session tokens by validating the refresh token and generating a new token pair.
/// </summary>
internal class RefreshTokensCommandHandler(
    IIdentityRepository<RefreshToken> refreshTokenRepo,
    IIdentityRepository<User> userRepository,
    ITokenService tokenService,
    ILogger<RefreshTokensCommandHandler> logger)
    : IRequestHandler<RefreshTokensCommand, Result<IdentityError, UserTokensDto>>
{
    /// <summary>
    /// Handles refreshing session tokens by validating the refresh token and generating a new token pair.
    /// </summary>
    public async Task<Result<IdentityError, UserTokensDto>> Handle(RefreshTokensCommand command, CancellationToken cancellationToken)
    {
        var storedRefreshToken = await GetValidRefreshTokenAsync(command.UserId, cancellationToken);

        var validation = Validate(command, storedRefreshToken);
        if (!validation.IsSuccess)
            return validation;

        var userInfo = await GetUserLoginInfoAsync(command.UserId, cancellationToken);

        if (userInfo == null || (userInfo.LockoutUntilUtc.HasValue && userInfo.LockoutUntilUtc > DateTime.UtcNow))
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.InvalidCredentials, "Invalid or locked user account.");

        var tokens = await tokenService.GenerateUserTokensAsync(userInfo, storedRefreshToken!.Id, cancellationToken);

        logger.LogInformation("Refresh token rotated successfully for user {UserId}", command.UserId);

        return Result<IdentityError, UserTokensDto>.Success(tokens);
    }

    #region Private Methods

    private Result<IdentityError, UserTokensDto> Validate(RefreshTokensCommand command, Data.Entites.RefreshToken? storedRefreshToken)
    {
        if (storedRefreshToken is null)
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.InvalidToken, "Refresh token not found or revoked.");

        if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.InvalidToken, "Refresh token expired.");

        bool isValid = tokenService.VerifyToken(command.RefreshToken, storedRefreshToken.TokenHash);
        if (!isValid)
            return Result<IdentityError, UserTokensDto>.Failure(IdentityError.InvalidToken, "Invalid refresh token.");

        return Result<IdentityError, UserTokensDto>.Success(default!);
    }

    private Task<Data.Entites.RefreshToken?> GetValidRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        return refreshTokenRepo.GetAll()
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<UserLoginInfoDto?> GetUserLoginInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        return userRepository.GetAll()
            .Include(u => u.UserProfile)
            .Where(u => u.Id == userId)
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

    #endregion
}
