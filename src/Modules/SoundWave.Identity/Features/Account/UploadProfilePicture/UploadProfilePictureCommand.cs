using MediatR;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Account.UploadProfilePicture;

/// <summary>
/// Command to validate and upload a user's avatar image, replacing any previously uploaded avatar.
/// </summary>
/// <param name="UserId">The identifier of the user whose avatar is being uploaded.</param>
/// <param name="Content">The file stream containing the image bytes.</param>
/// <param name="FileName">The original file name submitted by the client.</param>
/// <param name="ContentLength">The length of the payload in bytes.</param>
internal record UploadProfilePictureCommand(
    Guid UserId,
    Stream Content,
    string FileName,
    long ContentLength) : IRequest<Result<IdentityError, UploadImageResponse>>;
