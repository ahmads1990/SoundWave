using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SoundWave.SharedKernel.Interfaces;
using System.Text;

namespace SoundWave.SharedKernel.Services;

public class RabbitMqEventBus(
    IRabbitMqConnection connectionManager,
    ILogger<RabbitMqEventBus> logger)
    : IEventBus
{
    public async Task PublishAsync(
        string exchange,
        string routingKey,
        string payload,
        CancellationToken cancellationToken = default)
    {
        await using var channel = await connectionManager.CreateChannelAsync(cancellationToken);

        // Ensure target topic exchange exists (durable)
        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMqEventBus published message — exchange={Exchange} routingKey={RoutingKey}",
            exchange, routingKey);
    }
}
