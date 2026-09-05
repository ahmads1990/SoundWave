namespace SoundWave.Identity.Common;

internal class Constants
{
    internal const string SCHEMA_NAME = "Auth";
    internal static class Tags
    {
        internal const string Auth = "Auth";
    }

    /// <summary>
    /// The root directory containing the module's email templates at runtime.
    /// </summary>
    internal static readonly string TEMPLATE_ROOT = Path.Combine(
        Path.GetDirectoryName(typeof(IdentityModule).Assembly.Location)!,
        "Templates",
        "Identity"
    );

    internal const int MAX_FAILED_LOGIN_ATTEMPTS = 5;

    internal const int MAX_HARD_FAILED_LOGIN_ATTEMPTS = 10;

    internal const int SOFT_LOCKOUT_DURATION_MINUTES = 60;

    internal const int HARD_LOCKOUT_DURATION_YEARS = 100;

    internal static class Caching
    {
        private const string UserEmailVerificationPrefix = "userEmailVerify:";
        internal const int UserEmailVerificationTtlMinutes = 60;

        private const string UserFailedLoginPrefix = "userFailedLogin:";
        internal const int UserFailedLoginTtlMinutes = 5;

        private const string UserHardFailedLoginPrefix = "userHardFailedLogin:";
        internal const int UserHardFailedLoginTtlMinutes = 60 * 12;

        private const string UserPasswordResetPrefix = "userPasswordReset:";
        internal const int UserPasswordResetTtlMinutes = 60;

        internal static string GetUserEmailVerificationKey(Guid userId) => $"{UserEmailVerificationPrefix}{userId}";
        internal static string GetUserFailedLoginKey(Guid userId) => $"{UserFailedLoginPrefix}{userId}";
        internal static string GetUserHardFailedLoginKey(Guid userId) => $"{UserHardFailedLoginPrefix}{userId}";
        internal static string GetUserPasswordResetKey(Guid userId) => $"{UserPasswordResetPrefix}{userId}";
    }

    internal static class Email
    {
        internal static class Subjects
        {
            internal const string Welcome = "Welcome to SoundWave";
            internal const string VerifyEmail = "Verify your email address - SoundWave";
            internal const string PasswordReset = "Password Reset Request - SoundWave";
        }

        internal static class TemplateKeys
        {
            internal const string FullName = "FullName";
            internal const string Year = "Year";
            internal const string Otp = "Otp";
            internal const string ExpiresIn = "ExpiresIn";
        }
    }

    internal static class MessageBus
    {
        /// <summary>Topic exchange for all Identity module events.</summary>
        internal const string Exchange = "soundwave.identity";

        internal static class RoutingKeys
        {
            internal const string UserRegistered = "user.registered";
        }
    }
}
