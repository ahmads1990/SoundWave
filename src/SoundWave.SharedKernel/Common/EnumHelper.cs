namespace SoundWave.SharedKernel.Common;

/// <summary>
/// Utility helper for inspecting and formatting enum types and allowed values for API descriptions and metadata.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Formats all defined values of an enum type into a formatted string, e.g. "All (0), Playlists (1), Albums (2)".
    /// </summary>
    public static string ToAllowedValuesString<TEnum>() where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        return string.Join(", ", values.Select(v => $"{v} ({(int)(object)v})"));
    }

    /// <summary>
    /// Formats an array of allowed string or property names into a comma-separated list.
    /// </summary>
    public static string FormatAllowedValues(params string[] values)
    {
        return string.Join(", ", values);
    }
}
