using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active data-selection inputs resolved for a Cephalon app.
/// </summary>
public sealed class DataSelection
{
    /// <summary>
    /// Gets an empty data-selection instance.
    /// </summary>
    public static DataSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSelection" /> class.
    /// </summary>
    /// <param name="provider">The selected primary data-provider family or implementation identifier.</param>
    /// <param name="readWriteSplit">Whether distinct read and write paths were explicitly selected.</param>
    /// <param name="outboxEnabled">Whether the outbox pattern was explicitly enabled.</param>
    /// <param name="idGenerator">The selected identifier-generation strategy.</param>
    [JsonConstructor]
    public DataSelection(
        string? provider = null,
        bool? readWriteSplit = null,
        bool? outboxEnabled = null,
        string? idGenerator = null)
    {
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        ReadWriteSplit = readWriteSplit;
        OutboxEnabled = outboxEnabled;
        IdGenerator = string.IsNullOrWhiteSpace(idGenerator) ? null : idGenerator.Trim();
    }

    /// <summary>
    /// Gets the selected primary data-provider family or implementation identifier.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets a value indicating whether distinct read and write paths were explicitly selected.
    /// </summary>
    public bool? ReadWriteSplit { get; }

    /// <summary>
    /// Gets a value indicating whether the outbox pattern was explicitly enabled.
    /// </summary>
    public bool? OutboxEnabled { get; }

    /// <summary>
    /// Gets the selected identifier-generation strategy.
    /// </summary>
    public string? IdGenerator { get; }

    /// <summary>
    /// Gets a value indicating whether any data-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Provider is not null ||
        ReadWriteSplit.HasValue ||
        OutboxEnabled.HasValue ||
        IdGenerator is not null;
}
