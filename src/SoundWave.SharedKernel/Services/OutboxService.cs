using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Data.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;
using System.Text.Json;

namespace SoundWave.SharedKernel.Services;

public class OutboxService : IOutboxService
{
    public void WriteOutboxMessage(OutboxMessageRequest request, DbContext context)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Exchange = request.Exchange,
            RoutingKey = request.RoutingKey,
            Payload = JsonSerializer.Serialize(request.Payload),
            Sent = false,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.Add(outboxMessage);
    }
}

