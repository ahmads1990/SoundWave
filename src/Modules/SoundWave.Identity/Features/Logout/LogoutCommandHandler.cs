using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Services;

namespace SoundWave.Identity.Features.Logout;

/// <summary>
/// Handles logging out a user, revoking their refresh token and blacklisting the JTI.
/// </summary>
internal class LogoutCommandHandler(
    ITokenService tokenService,
    ILogger<LogoutCommandHandler> logger)
    : IRequestHandler<LogoutCommand, IdentityResult<bool>>
{
    /// <summary>
    /// Handles the logout command request.
    /// </summary>
    /// <param name="command">The logout command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An identity result.</returns>
    public async Task<IdentityResult<bool>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var validationResult = Validate(command, cancellationToken);
        if (!validationResult.IsSuccess)
            return validationResult;

        await tokenService.RevokeActiveRefreshToken(command.UserId, cancellationToken);

        if (command.ExpiryDate.HasValue)
        {
            await tokenService.BlacklistJtiAsync(command.Jti, command.ExpiryDate.Value, cancellationToken);
        }

        return IdentityResult<bool>.Success(true);
    }

    #region Private Methods

    private IdentityResult<bool> Validate(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (Guid.Empty == command.UserId)
        {
            return IdentityResult<bool>.Failure(IdentityError.InvalidToken,"User ID cannot be empty.");
        }
        if (string.IsNullOrEmpty(command.Jti))
        {
            return IdentityResult<bool>.Failure(IdentityError.InvalidToken, "JTI cannot be null or empty.");
        }
        return IdentityResult<bool>.Success(true);
    }

    #endregion
}
