using System.Text.Json.Serialization;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes one versioned, operator-facing extension section in the combined runtime snapshot.
/// </summary>
public sealed class RuntimeIntrospectionSection
{
    /// <summary>
    /// Creates a runtime introspection extension section.
    /// </summary>
    /// <param name="id">The globally stable section identifier.</param>
    /// <param name="schemaVersion">The version of the section payload contract.</param>
    /// <param name="source">The package or subsystem that owns the section.</param>
    /// <param name="displayName">The operator-facing section name.</param>
    /// <param name="description">A human-readable explanation of the section.</param>
    /// <param name="entries">The current entries projected by the section.</param>
    [JsonConstructor]
    public RuntimeIntrospectionSection(
        string id,
        string schemaVersion,
        string source,
        string displayName,
        string description,
        IReadOnlyList<RuntimeIntrospectionSectionEntry>? entries = null)
    {
        Id = RequireValue(id, nameof(id), "Section id is required.");
        SchemaVersion = RequireValue(schemaVersion, nameof(schemaVersion), "Section schema version is required.");
        Source = RequireValue(source, nameof(source), "Section source is required.");
        DisplayName = RequireValue(displayName, nameof(displayName), "Section display name is required.");
        Description = RequireValue(description, nameof(description), "Section description is required.");

        var orderedEntries = (entries ?? Array.Empty<RuntimeIntrospectionSectionEntry>())
            .OrderBy(static entry => entry.Id, StringComparer.Ordinal)
            .ToArray();
        var duplicateEntryId = orderedEntries
            .GroupBy(static entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1)?.Key;

        if (duplicateEntryId is not null)
        {
            throw new ArgumentException(
                $"Runtime introspection section '{Id}' contains duplicate entry id '{duplicateEntryId}'.",
                nameof(entries));
        }

        Entries = orderedEntries;
    }

    /// <summary>Gets the globally stable section identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the version of the section payload contract.</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the package or subsystem that owns the section.</summary>
    public string Source { get; }

    /// <summary>Gets the operator-facing section name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the human-readable explanation of the section.</summary>
    public string Description { get; }

    /// <summary>Gets the deterministically ordered entries projected by the section.</summary>
    public IReadOnlyList<RuntimeIntrospectionSectionEntry> Entries { get; }

    private static string RequireValue(string value, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }
}
