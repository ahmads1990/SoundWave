namespace SoundWave.SharedKernel.Interfaces.Jobs;

/// <summary>
/// Hangfire recurring job contract for scavenging abandoned file uploads.
/// </summary>
public interface ICleanupStaleUploadsJob
{
    /// <summary>
    /// Executes the stale upload cleanup process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
