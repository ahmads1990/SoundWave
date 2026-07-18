using MediatR;
using SoundWave.Identity.Common;

using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Logout;

/// <summary>
/// Command for logging out a user by revoking their refresh token and blacklisting the JTI.
/// </summary>
internal record LogoutCommand(Guid UserId, string Jti, DateTime? ExpiryDate) : IRequest<Result<IdentityError, bool>>;
