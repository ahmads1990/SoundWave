using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Configs;

/// <summary>
/// Configuration settings for the Hangfire <c>CleanupStaleUploadsJob</c>.
/// Controls recurring execution schedule, cutoff window, and job identifier.
/// </summary>
public sealed class CleanupStaleUploadsConfig
{
    /// <summary>
    /// Configuration section path in <c>appsettings.json</c>.
    /// </summary>
    public const string SectionPath = SharedConstants.Jobs.CleanupStaleUploadsSection;

    /// <summary>
    /// Unique Hangfire recurring job identifier.
    /// Defaults to <c>cleanup-stale-uploads</c>.
    /// </summary>
    public string JobId { get; set; } = "cleanup-stale-uploads";

    /// <summary>
    /// Cron expression defining when the cleanup job executes.
    /// Defaults to daily at 03:00 UTC (<c>0 3 * * *</c>).
    /// </summary>
    public string CronExpression { get; set; } = "0 3 * * *";

    /// <summary>
    /// Age threshold in hours beyond which a pending upload is considered abandoned.
    /// Defaults to 24 hours. Overridden by <see cref="CutoffDays"/> if specified.
    /// </summary>
    public int CutoffHours { get; set; } = 24;

    /// <summary>
    /// Optional age threshold in days. When set, takes precedence over <see cref="CutoffHours"/>.
    /// </summary>
    public int? CutoffDays { get; set; }

    /// <summary>
    /// Calculates the effective cutoff duration based on configured days or hours.
    /// </summary>
    /// <returns>A <see cref="TimeSpan"/> representing the configured age threshold.</returns>
    public TimeSpan GetCutoffTimeSpan() =>
        CutoffDays.HasValue && CutoffDays.Value > 0
            ? TimeSpan.FromDays(CutoffDays.Value)
            : TimeSpan.FromHours(CutoffHours > 0 ? CutoffHours : 24);
}
