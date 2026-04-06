using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.RabbitMqDependencies.Configuration;

/// <summary>
/// Describes one RabbitMQ dependency that should contribute to runtime health.
/// </summary>
public sealed class RabbitMqDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the optional AMQP connection string used for the probe.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the RabbitMQ host name or IP address to probe when no full connection string is supplied.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the RabbitMQ TCP port.</summary>
    public int Port { get; set; } = 5672;

    /// <summary>Gets or sets the RabbitMQ virtual host used for the probe connection.</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Gets or sets the optional user name used for authentication when no full connection string is supplied.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional password used for authentication when no full connection string is supplied.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets a value indicating whether TLS should be enabled for the broker probe.</summary>
    public bool UseTls { get; set; }
}
