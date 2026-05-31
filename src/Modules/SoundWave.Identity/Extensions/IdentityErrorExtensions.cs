using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Extensions;

internal static class IdentityErrorExtensions
{
    public static ApiErrorCode ToApiErrorCode(this IdentityError error) => error switch
    {
        IdentityError.InvalidCredentials => ApiErrorCode.InvalidCredentials,
        IdentityError.EmailNotVerified => ApiErrorCode.EmailNotVerified,
        IdentityError.EmailAlreadyExists => ApiErrorCode.EmailAlreadyExists,
        IdentityError.AccountLocked => ApiErrorCode.Unauthorized,
        IdentityError.InvalidToken => ApiErrorCode.InvalidToken,
        IdentityError.UserNotFound => ApiErrorCode.ResourceNotFound,
        IdentityError.InternalError => ApiErrorCode.InternalServerError,
        _ => ApiErrorCode.InternalServerError,
    };
}
