using MediatR;
using SoundWave.Identity.Common;

using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.VerifyEmail;

/// <summary>
/// Command for verifying a user's email address using an OTP.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Otp">The one-time password sent to the user's email.</param>
internal record VerifyEmailCommand(string Email, string Otp) : IRequest<Result<IdentityError, bool>>;
