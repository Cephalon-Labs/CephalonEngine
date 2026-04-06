using Confluent.Kafka;

namespace Cephalon.Behaviors.Messaging.Options;

/// <summary>
/// Configuration options for the Kafka messaging transport binding.
/// </summary>
public sealed class KafkaTransportOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers (comma-separated host:port pairs).
    /// Default: <c>"localhost:9092"</c>.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group id. Default: <c>"cephalon-behaviors"</c>.
    /// </summary>
    public string GroupId { get; set; } = "cephalon-behaviors";

    /// <summary>
    /// Gets or sets the Kafka topic to subscribe to.
    /// When <see langword="null" />, defaults to the behavior id.
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Gets or sets the auto offset reset policy applied when no committed offset exists.
    /// Default: <see cref="Confluent.Kafka.AutoOffsetReset.Earliest" />.
    /// </summary>
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;

    /// <summary>
    /// Gets or sets a value indicating whether Kafka auto-commit is enabled.
    /// Default: <see langword="false" /> — offsets are committed manually after successful dispatch.
    /// </summary>
    public bool EnableAutoCommit { get; set; }
}
