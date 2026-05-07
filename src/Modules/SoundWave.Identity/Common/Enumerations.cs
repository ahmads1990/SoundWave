namespace SoundWave.Identity.Common;

internal enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2,
}

public enum IdentityOperationResult
{
    Success,
    UserNotFound,
    Unauthorized
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
