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
    AccountLocked,
    InvalidToken,
    UserNotFound,
    InternalError,
}

public enum UserRole
{
    Listener,
    Artist,
    Admin
}

public enum EmailTemplates
{
    Welcome,
}
