namespace SoundWave.SharedKernel.Models;

public class OutboxMessageRequest
{
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public object Payload { get; set; } = default!;
}
