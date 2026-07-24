using RabbitMQ.Client;

namespace SoundWave.SharedKernel.Interfaces;

public interface IRabbitMqConnection : IAsyncDisposable
{
    ValueTask<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);
}
