using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Services;

public class RabbitMqConnection : IRabbitMqConnection
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnection> _logger;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqConnection(IOptions<RabbitMqConfig> config, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;
        var settings = config.Value;

        _factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost
        };
    }

    public async ValueTask<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not { IsOpen: true })
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_connection is not { IsOpen: true })
                {
                    _logger.LogInformation("Establishing connection to RabbitMQ at {Host}:{Port}...", _factory.HostName, _factory.Port);
                    _connection = await _factory.CreateConnectionAsync(cancellationToken);
                    _logger.LogInformation("Successfully connected to RabbitMQ");
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        return await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
