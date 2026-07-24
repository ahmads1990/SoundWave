using Microsoft.EntityFrameworkCore;
using SoundWave.SharedKernel.Data.Entities;
using SoundWave.SharedKernel.Models;

namespace SoundWave.SharedKernel.Interfaces;

public interface IOutboxService
{
    /// <summary>
    /// Stages an outbox message on the provided <paramref name="context"/>.
    /// Must be called before <c>SaveChangesAsync</c> so the message is persisted
    /// in the same transaction as the caller's business data.
    /// </summary>
    void WriteOutboxMessage(OutboxMessageRequest request, DbContext context);
}
