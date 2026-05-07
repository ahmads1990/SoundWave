namespace SoundWave.SharedKernel.Models;

/// <summary>
/// Encapsulates the parameters required to send an email.
/// </summary>
public class EmailRequest
{
    public string ToName { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public Dictionary<string, string> TemplateModel { get; set; } = [];
}
