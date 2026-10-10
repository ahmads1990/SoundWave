using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Identity.Features.Account.UploadProfilePicture;

/// <summary>
/// Handles uploading and persisting the user's avatar image.
/// </summary>
internal class UploadProfilePictureCommandHandler(
    IIdentityRepository<UserProfile> userProfileRepository,
    IImageUploadService imageUploadService,
    ILogger<UploadProfilePictureCommandHandler> logger)
    : IRequestHandler<UploadProfilePictureCommand, Result<IdentityError, UploadImageResponse>>
{
    public async Task<Result<IdentityError, UploadImageResponse>> Handle(
        UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<IdentityError, UploadImageResponse>.Failure(validation.Error, validation.ErrorMessage);

        var profile = validation.Data!;
        var previousAvatarUrl = profile.ProfilePicUrl;

        var uploadResult = await imageUploadService.ReplaceImageAsync(
            request.Content,
            request.FileName,
            FileType.ProfilePic,
            previousAvatarUrl,
            cancellationToken: cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            logger.LogWarning("Profile avatar upload failed for user {UserId}: {Reason}", request.UserId, uploadResult.ErrorMessage);
            return Result<IdentityError, UploadImageResponse>.Failure(IdentityError.FileValidationFailed, uploadResult.ErrorMessage);
        }

        profile.ProfilePicUrl = uploadResult.Data!.PublicUrl;
        userProfileRepository.SaveInclude(profile, nameof(UserProfile.ProfilePicUrl));
        await userProfileRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Profile avatar updated successfully for user {UserId}", request.UserId);

        return Result<IdentityError, UploadImageResponse>.Success(
            new UploadImageResponse(uploadResult.Data.FileId, uploadResult.Data.PublicUrl, uploadResult.Data.SizeBytes));
    }

    #region Private Methods

    private async Task<Result<IdentityError, UserProfile>> ValidateAsync(
        UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByCondition(p => p.UserId == request.UserId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("Avatar upload rejected — profile not found for user {UserId}", request.UserId);
            return Result<IdentityError, UserProfile>.Failure(IdentityError.UserNotFound, "User profile not found.");
        }

        return Result<IdentityError, UserProfile>.Success(profile);
    }

    #endregion
}
