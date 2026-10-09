using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Configs;

/// <summary>
/// Configuration settings for the Hangfire <c>ReconcileStaleValidationsJob</c>.
/// Controls recurring execution schedule, stale timeout threshold, and job identifier.
/// </summary>
public sealed class ReconcileStaleValidationsConfig
{
    /// <summary>
    /// Configuration section path in <c>appsettings.json</c>.
    /// </summary>
    public const string SectionPath = SharedConstants.Jobs.ReconcileStaleValidationsSection;

    /// <summary>
    /// Unique Hangfire recurring job identifier.
    /// Defaults to <c>reconcile-stale-validations</c>.
    /// </summary>
    public string JobId { get; set; } = "reconcile-stale-validations";

    /// <summary>
    /// Cron expression defining when the reconciliation job executes.
    /// Defaults to every 15 minutes (<c>*/15 * * * *</c>).
    /// </summary>
    public string CronExpression { get; set; } = "*/15 * * * *";

    /// <summary>
    /// Timeout in minutes beyond which an upload stuck in PendingValidation is considered stale.
    /// Defaults to 30 minutes. Overridden by <see cref="StaleTimeoutHours"/> if specified.
    /// </summary>
    public int StaleTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Optional timeout in hours. When set, takes precedence over <see cref="StaleTimeoutMinutes"/>.
    /// </summary>
    public int? StaleTimeoutHours { get; set; }

    /// <summary>
    /// Calculates the effective stale validation cutoff duration based on configured hours or minutes.
    /// </summary>
    /// <returns>A <see cref="TimeSpan"/> representing the configured timeout threshold.</returns>
    public TimeSpan GetCutoffTimeSpan() =>
        StaleTimeoutHours.HasValue && StaleTimeoutHours.Value > 0
            ? TimeSpan.FromHours(StaleTimeoutHours.Value)
            : TimeSpan.FromMinutes(StaleTimeoutMinutes > 0 ? StaleTimeoutMinutes : 30);
}
