namespace SoundWave.SharedKernel.Interfaces.Jobs;

/// <summary>
/// Hangfire recurring job contract for reconciling stuck pending file validations.
/// </summary>
public interface IReconcileStaleValidationsJob
{
    /// <summary>
    /// Executes the stale validation reconciliation process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
