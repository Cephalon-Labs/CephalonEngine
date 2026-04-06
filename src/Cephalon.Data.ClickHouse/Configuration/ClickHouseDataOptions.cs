namespace Cephalon.Data.ClickHouse.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.ClickHouse</c> connects to ClickHouse and registers data services.
/// </summary>
public sealed class ClickHouseDataOptions
{
    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "clickhouse";

    /// <summary>The ClickHouse host (e.g. <c>"localhost"</c>).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>The ClickHouse HTTP port. Defaults to <c>8123</c>.</summary>
    public int Port { get; set; } = 8123;

    /// <summary>The ClickHouse database. Defaults to <c>"default"</c>.</summary>
    public string Database { get; set; } = "default";

    /// <summary>ClickHouse username. Defaults to <c>"default"</c>.</summary>
    public string Username { get; set; } = "default";

    /// <summary>ClickHouse password. Defaults to empty.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional prefix applied to all managed table names.</summary>
    public string TablePrefix { get; set; } = "cephalon_";

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IOutbox" /> backed by a ClickHouse ReplacingMergeTree table.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IInbox" /> backed by a ClickHouse ReplacingMergeTree table.</summary>
    public bool RegisterInbox { get; set; }

    /// <summary>Builds a ClickHouse ADO.NET connection string from the configured options.</summary>
    public string BuildConnectionString() =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}
