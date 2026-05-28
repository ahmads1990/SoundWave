using MediatR;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Represents the internal MediatR command for authenticating a user.
/// </summary>
/// <param name="Email">The email address of the user attempting to log in.</param>
/// <param name="Password">The plain-text password of the user attempting to log in.</param>
internal record LoginCommand(
    string Email,
    string Password
    ) : IRequest<BaseApiResponse<UserTokensDto>>;