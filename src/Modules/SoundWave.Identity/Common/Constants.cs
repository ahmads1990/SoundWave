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

    internal static class Email
    {
        internal static class Subjects
        {
            internal const string Welcome = "Welcome to SoundWave";
        }

        internal static class TemplateKeys
        {
            internal const string FullName = "FullName";
            internal const string Year = "Year";
        }
    }
}
