using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.GetMyProfile;

/// <summary>
/// Handles retrieving the current user's profile information.
/// </summary>
internal class GetMyProfileQueryHandler(
    IIdentityRepository<UserProfile> userProfileRepository,
    ILogger<GetMyProfileQueryHandler> logger)
    : IRequestHandler<GetMyProfileQuery, Result<IdentityError, UserProfileDto>>
{
    public async Task<Result<IdentityError, UserProfileDto>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByCondition(p => p.UserId == request.UserId)
            .Include(p => p.User)
            .Select(p => new UserProfileDto(
                p.Id,
                p.UserId,
                p.FirstName,
                p.LastName,
                p.DisplayName,
                p.User.Email,
                p.ProfilePicUrl,
                p.CoverImageUrl,
                p.PhoneNumber,
                p.DateOfBirth,
                p.Language,
                p.Gender.ToString(),
                p.CountryId))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("GetMyProfile failed: Profile for UserId {UserId} not found", request.UserId);
            return Result<IdentityError, UserProfileDto>.Failure(IdentityError.UserNotFound, "User profile not found.");
        }

        return Result<IdentityError, UserProfileDto>.Success(profile);
    }
}
