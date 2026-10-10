using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Identity.Features.Account.UploadProfileBanner;

/// <summary>
/// Handles uploading and persisting the user's banner/cover image.
/// </summary>
internal class UploadProfileBannerCommandHandler(
    IIdentityRepository<UserProfile> userProfileRepository,
    IImageUploadService imageUploadService,
    ILogger<UploadProfileBannerCommandHandler> logger)
    : IRequestHandler<UploadProfileBannerCommand, Result<IdentityError, UploadImageResponse>>
{
    public async Task<Result<IdentityError, UploadImageResponse>> Handle(
        UploadProfileBannerCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<IdentityError, UploadImageResponse>.Failure(validation.Error, validation.ErrorMessage);

        var profile = validation.Data!;
        var previousBannerUrl = profile.CoverImageUrl;

        var uploadResult = await imageUploadService.ReplaceImageAsync(
            request.Content,
            request.FileName,
            FileType.ProfileBanner,
            previousBannerUrl,
            cancellationToken: cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            logger.LogWarning("Profile banner upload failed for user {UserId}: {Reason}", request.UserId, uploadResult.ErrorMessage);
            return Result<IdentityError, UploadImageResponse>.Failure(IdentityError.FileValidationFailed, uploadResult.ErrorMessage);
        }

        profile.CoverImageUrl = uploadResult.Data!.PublicUrl;
        userProfileRepository.SaveInclude(profile, nameof(UserProfile.CoverImageUrl));
        await userProfileRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Profile banner updated successfully for user {UserId}", request.UserId);

        return Result<IdentityError, UploadImageResponse>.Success(
            new UploadImageResponse(uploadResult.Data.FileId, uploadResult.Data.PublicUrl, uploadResult.Data.SizeBytes));
    }

    #region Private Methods

    private async Task<Result<IdentityError, UserProfile>> ValidateAsync(
        UploadProfileBannerCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByCondition(p => p.UserId == request.UserId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("Banner upload rejected — profile not found for user {UserId}", request.UserId);
            return Result<IdentityError, UserProfile>.Failure(IdentityError.UserNotFound, "User profile not found.");
        }

        return Result<IdentityError, UserProfile>.Success(profile);
    }

    #endregion
}
