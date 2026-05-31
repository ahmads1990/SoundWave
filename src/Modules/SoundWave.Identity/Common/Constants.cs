namespace SoundWave.Identity.Common;

internal class Constants
{
    internal const string SCHEMA_NAME = "Identity";
    internal const string MODULE_TAG = "Identity";

    /// <summary>
    /// The root directory containing the module's email templates at runtime.
    /// </summary>
    internal static readonly string TEMPLATE_ROOT = Path.Combine(
        Path.GetDirectoryName(typeof(IdentityModule).Assembly.Location)!,
        "Templates",
        MODULE_TAG
    );

    internal const int MAX_FAILED_LOGIN_ATTEMPTS = 5;

    internal static class Caching
    {
        internal const string UserEmailVerification = "userEmailVerify:";
        internal const int UserEmailVerificationTtlMinutes = 60;

        internal const string UserFailedLogin = "userFailedLogin:";
        internal const int UserFailedLoginTtlMinutes = 60 * 5;
    }

    internal static class Email
    {
        internal static class Subjects
        {
            internal const string Welcome = "Welcome to SoundWave";
            internal const string VerifyEmail = "Verify your email address - SoundWave";
        }

        internal static class TemplateKeys
        {
            internal const string FullName = "FullName";
            internal const string Year = "Year";
            internal const string Otp = "Otp";
        }
    }
}
