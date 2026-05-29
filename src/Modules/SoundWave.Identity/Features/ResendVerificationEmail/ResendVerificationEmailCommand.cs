using MediatR;
using SoundWave.Identity.Common;

namespace SoundWave.Identity.Features.ResendVerificationEmail;

/// <summary>
/// Command for resending the email verification OTP to a user.
/// </summary>
/// <param name="Email">The user's email address.</param>
internal record ResendVerificationEmailCommand(string Email) : IRequest<IdentityResult<bool>>;
