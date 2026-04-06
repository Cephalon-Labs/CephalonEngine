using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.KafkaDependencies.Configuration;

/// <summary>
/// Describes one Kafka dependency that should contribute to runtime health.
/// </summary>
public sealed class KafkaDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the Kafka bootstrap server list, such as <c>broker-1:9092,broker-2:9092</c>.</summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional client identifier sent to the Kafka cluster.</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the optional topic name that should be present in returned cluster metadata.</summary>
    public string? Topic { get; set; }

    /// <summary>Gets or sets the optional Kafka security protocol, such as <c>Plaintext</c>, <c>Ssl</c>, <c>SaslPlaintext</c>, or <c>SaslSsl</c>.</summary>
    public string? SecurityProtocol { get; set; }

    /// <summary>Gets or sets the optional SASL mechanism, such as <c>Plain</c>, <c>ScramSha256</c>, or <c>ScramSha512</c>.</summary>
    public string? SaslMechanism { get; set; }

    /// <summary>Gets or sets the optional SASL user name used when authenticated broker access is required.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional SASL password used when authenticated broker access is required.</summary>
    public string? Password { get; set; }
}
