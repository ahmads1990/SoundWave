namespace SoundWave.SharedKernel.Interfaces;

/// <summary>
/// Abstraction over the message broker. The worker uses this to publish
/// outbox messages. Swap the implementation in DI to switch brokers.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a pre-serialised JSON payload to a topic exchange.
    /// </summary>
    Task PublishAsync(
        string exchange,
        string routingKey,
        string payload,
        CancellationToken cancellationToken = default);
}
