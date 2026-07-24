using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Data;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Jobs;

/// <summary>
/// Background worker that polls <see cref="SharedDbContext.OutboxMessages"/> and publishes
/// unsent messages to the message broker via <see cref="IEventBus"/>.
/// Retries up to <see cref="SharedConstants.Outbox.MaxRetries"/> times before dead-lettering.
/// </summary>
public sealed class OutboxProcessorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessorWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let an unexpected error kill the worker
                logger.LogError(ex, "OutboxProcessorWorker encountered an unhandled exception");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("OutboxProcessorWorker stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        // New scope per batch — BackgroundService is Singleton, DbContext is Scoped
        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await db.OutboxMessages
            .Where(m => !m.Sent && !m.IsDeadLetter && m.RetryCount < SharedConstants.Outbox.MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        logger.LogInformation("OutboxProcessorWorker processing {Count} message(s)", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await eventBus.PublishAsync(
                    exchange: message.Exchange,
                    routingKey: message.RoutingKey,
                    payload: message.Payload,
                    cancellationToken: cancellationToken);

                message.Sent = true;
                message.ProcessedAt = DateTime.UtcNow;

                logger.LogInformation(
                    "OutboxMessage {Id} published — exchange={Exchange} routingKey={RoutingKey}",
                    message.Id, message.Exchange, message.RoutingKey);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= SharedConstants.Outbox.MaxRetries)
                {
                    message.IsDeadLetter = true;

                    logger.LogError(
                        "OutboxMessage {Id} is dead after {MaxRetries} retries — exchange={Exchange} routingKey={RoutingKey} error={Error}",
                        message.Id, SharedConstants.Outbox.MaxRetries, message.Exchange, message.RoutingKey, ex.Message);
                }
                else
                {
                    logger.LogWarning(
                        "OutboxMessage {Id} failed (attempt {RetryCount}/{MaxRetries}) — {Error}",
                        message.Id, message.RetryCount, SharedConstants.Outbox.MaxRetries, ex.Message);
                }
            }
        }

        // Single SaveChanges for the whole batch — marks all successes and failures in one write
        await db.SaveChangesAsync(cancellationToken);
    }
}
