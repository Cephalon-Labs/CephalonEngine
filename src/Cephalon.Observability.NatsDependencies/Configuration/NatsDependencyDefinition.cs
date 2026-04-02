namespace Cephalon.Observability.NatsDependencies.Configuration;

/// <summary>
/// Describes one NATS dependency that should contribute to runtime health.
/// </summary>
public sealed class NatsDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NatsDependencyDefinition" /> class.
    /// </summary>
    public NatsDependencyDefinition()
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
    /// Gets or sets the NATS host name or IP address to probe.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the NATS client port.
    /// </summary>
    public int Port { get; set; } = 4222;

    /// <summary>
    /// Gets or sets a value indicating whether the probe should upgrade the connection to TLS after reading the initial server info line.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets the TLS server name used for certificate validation when <see cref="UseTls" /> is enabled.
    /// </summary>
    public string? TlsServerName { get; set; }

    /// <summary>
    /// Gets or sets the optional user name used for NATS user/password authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for NATS user/password authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the optional auth token used for token-based NATS authentication.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the optional client name sent in the NATS <c>CONNECT</c> payload.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
