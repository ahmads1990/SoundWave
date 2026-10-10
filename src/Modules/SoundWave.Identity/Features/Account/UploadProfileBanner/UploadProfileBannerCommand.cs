using MediatR;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Account.UploadProfileBanner;

/// <summary>
/// Command to validate and upload a user's banner/cover image, replacing any previously uploaded banner.
/// </summary>
/// <param name="UserId">The identifier of the user whose banner is being uploaded.</param>
/// <param name="Content">The file stream containing the image bytes.</param>
/// <param name="FileName">The original file name submitted by the client.</param>
/// <param name="ContentLength">The length of the payload in bytes.</param>
internal record UploadProfileBannerCommand(
    Guid UserId,
    Stream Content,
    string FileName,
    long ContentLength) : IRequest<Result<IdentityError, UploadImageResponse>>;
