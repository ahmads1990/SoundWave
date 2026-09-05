using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.UpdateProfileImages;

/// <summary>
/// Handles updating the profile picture and cover image URLs for a user.
/// </summary>
internal class UpdateProfileImagesCommandHandler(
    IIdentityRepository<UserProfile> userProfileRepository,
    ILogger<UpdateProfileImagesCommandHandler> logger)
    : IRequestHandler<UpdateProfileImagesCommand, Result<IdentityError, Unit>>
{
    public async Task<Result<IdentityError, Unit>> Handle(
        UpdateProfileImagesCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByCondition(p => p.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("Profile update failed: UserProfile for UserId {UserId} not found", request.UserId);
            return Result<IdentityError, Unit>.Failure(IdentityError.UserNotFound, "User profile not found.");
        }

        profile.ProfilePicUrl = request.ProfilePicUrl;
        profile.CoverImageUrl = request.CoverImageUrl;

        userProfileRepository.SaveInclude(profile, nameof(UserProfile.ProfilePicUrl), nameof(UserProfile.CoverImageUrl));
        await userProfileRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Updated profile images for UserId {UserId}", request.UserId);
        return Result<IdentityError, Unit>.Success(Unit.Value);
    }
}
