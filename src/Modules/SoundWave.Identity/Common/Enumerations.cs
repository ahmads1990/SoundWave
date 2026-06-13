namespace SoundWave.Identity.Common;

internal enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2,
}

internal enum IdentityError
{
    None = 0,
    InvalidCredentials,
    EmailNotVerified,
    EmailAlreadyExists,
    AccountTemporarilyLocked,
    AccountLocked,
    InvalidToken,
    UserNotFound,
    EmailAlreadyVerified,
    InternalError,
}



public enum EmailTemplates
{
    Welcome,
    VerifyEmail,
}
