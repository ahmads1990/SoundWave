using MediatR;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.GetMyProfile;

/// <summary>
/// Query to retrieve the current user's profile details.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
internal record GetMyProfileQuery(Guid UserId) : IRequest<Result<IdentityError, UserProfileDto>>;
