namespace Cephalon.Data.Debezium.Configuration;

/// <summary>
/// Configuration options for the Debezium-managed external CDC pack (<c>Engine:Data:Debezium</c>).
/// </summary>
public sealed class DebeziumDataOptions
{
    /// <summary>
    /// Gets the default configuration section path for Debezium data settings.
    /// </summary>
    public const string SectionPath = "Engine:Data:Debezium";

    /// <summary>
    /// Gets the canonical provider identifier emitted by the pack.
    /// </summary>
    public const string ProviderId = "debezium";

    /// <summary>
    /// Gets the Debezium-managed connector runtimes that should contribute captures and external execution ownership to the active runtime.
    /// </summary>
    public IList<DebeziumConnectorOptions> Connectors { get; } = [];
}
