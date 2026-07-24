namespace SoundWave.SharedKernel.Data.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public bool Sent { get; set; }
    public int RetryCount { get; set; }
    public bool IsDeadLetter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

