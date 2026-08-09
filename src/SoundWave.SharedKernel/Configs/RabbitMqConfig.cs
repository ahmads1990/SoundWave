namespace SoundWave.SharedKernel.Configs;

public class RabbitMqConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public RabbitMqRetryConfig Retry { get; set; } = new();
}

public class RabbitMqRetryConfig
{
    public int RetryLimit { get; set; } = 5;
    public int MinIntervalSeconds { get; set; } = 1;
    public int MaxIntervalSeconds { get; set; } = 30;
    public int IntervalDeltaSeconds { get; set; } = 2;
}
