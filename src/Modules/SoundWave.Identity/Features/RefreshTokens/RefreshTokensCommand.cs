using MediatR;
using SoundWave.SharedKernel.Models.Responses;
using SoundWave.Identity.Common;
using SoundWave.Identity.Dtos;
using SoundWave.Identity.Features.Login;

namespace SoundWave.Identity.Features.RefreshTokens;

/// <summary>
/// Represents the internal MediatR command for refreshing session tokens.
/// </summary>
/// <param name="UserId">The ID of the user requesting the refresh.</param>
/// <param name="RefreshToken">The refresh token provided by the client.</param>
internal record RefreshTokensCommand(
    Guid UserId,
    string RefreshToken
    ) : IRequest<IdentityResult<UserTokensDto>>;
