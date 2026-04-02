namespace Cephalon.Observability.MqttDependencies.Configuration;

/// <summary>
/// Describes one MQTT dependency that should contribute to runtime health.
/// </summary>
public sealed class MqttDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MqttDependencyDefinition" /> class.
    /// </summary>
    public MqttDependencyDefinition()
    {
    }

    /// <summary>
    /// Gets or sets the stable dependency identifier surfaced through runtime health endpoints.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable dependency name shown to operators.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the MQTT host name or IP address to probe.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT broker port.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets a value indicating whether the probe should use TLS immediately after opening the TCP connection.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets the TLS server name used for certificate validation when <see cref="UseTls" /> is enabled.
    /// </summary>
    public string? TlsServerName { get; set; }

    /// <summary>
    /// Gets or sets the MQTT client identifier sent in the <c>CONNECT</c> packet.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the optional user name used for MQTT username/password authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for MQTT username/password authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the MQTT keep-alive interval, in seconds, advertised through the <c>CONNECT</c> packet.
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
