namespace Cephalon.Behaviors.Messaging.Options;

/// <summary>
/// Configuration options for the RabbitMQ messaging transport binding.
/// </summary>
public sealed class RabbitMqTransportOptions
{
    /// <summary>
    /// Gets or sets the RabbitMQ broker hostname. Default: <c>"localhost"</c>.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the RabbitMQ broker port. Default: <c>5672</c>.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the RabbitMQ virtual host. Default: <c>"/"</c>.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the RabbitMQ user name. Default: <c>"guest"</c>.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the RabbitMQ password. Default: <c>"guest"</c>.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the queue name. When <see langword="null" />, defaults to the behavior id.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the exchange name. When <see langword="null" />, uses the default exchange.
    /// </summary>
    public string? ExchangeName { get; set; }

    /// <summary>
    /// Gets or sets the dead-letter exchange name for messages that exceed max retry attempts.
    /// When <see langword="null" />, dead-lettering is disabled.
    /// </summary>
    public string? DeadLetterExchange { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of delivery attempts before a message is nacked without requeue.
    /// Default: <c>3</c>.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in milliseconds between retry attempts. Default: <c>1000</c>.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}
