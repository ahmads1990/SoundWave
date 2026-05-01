using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Extensions;

public static class IdentityOperationResultExtensions
{
    public static ApiErrorCode ToApiErrorCode(this IdentityOperationResult status) => status switch
    {
        IdentityOperationResult.UserNotFound  => ApiErrorCode.ResourceNotFound,
        IdentityOperationResult.Unauthorized  => ApiErrorCode.Unauthorized,
        _                            => ApiErrorCode.InternalServerError
    };
}
