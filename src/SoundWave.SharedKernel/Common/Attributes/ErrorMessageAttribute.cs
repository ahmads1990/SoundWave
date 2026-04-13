namespace SoundWave.SharedKernel.Common.Attributes;

/// <summary>
/// Custom attribute for providing human-readable error messages for enum fields.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ErrorMessageAttribute : Attribute
{
    public string Message { get; }

    public ErrorMessageAttribute(string message)
    {
        Message = message;
    }
}
