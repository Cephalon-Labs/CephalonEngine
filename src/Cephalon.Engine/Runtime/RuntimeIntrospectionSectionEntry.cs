using System.Text.Json.Serialization;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes one desired-versus-observed runtime entry inside an introspection extension section.
/// </summary>
public sealed class RuntimeIntrospectionSectionEntry
{
    /// <summary>
    /// Creates a runtime introspection section entry.
    /// </summary>
    /// <param name="id">The stable entry identifier within its section.</param>
    /// <param name="displayName">The operator-facing entry name.</param>
    /// <param name="description">A human-readable explanation of the entry.</param>
    /// <param name="desiredState">The state the runtime is expected to maintain.</param>
    /// <param name="observedState">The state most recently observed by the contributor.</param>
    /// <param name="conditions">The current operator conditions for the entry.</param>
    /// <param name="actions">The actions the owning subsystem declares for the entry.</param>
    /// <param name="metadata">Additional stable metadata for operator tooling.</param>
    [JsonConstructor]
    public RuntimeIntrospectionSectionEntry(
        string id,
        string displayName,
        string description,
        string desiredState,
        string observedState,
        IReadOnlyList<RuntimeOperatorCondition>? conditions = null,
        IReadOnlyList<RuntimeOperatorAction>? actions = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = RequireValue(id, nameof(id), "Entry id is required.");
        DisplayName = RequireValue(displayName, nameof(displayName), "Entry display name is required.");
        Description = RequireValue(description, nameof(description), "Entry description is required.");
        DesiredState = RequireValue(desiredState, nameof(desiredState), "Desired state is required.");
        ObservedState = RequireValue(observedState, nameof(observedState), "Observed state is required.");
        Conditions = (conditions ?? Array.Empty<RuntimeOperatorCondition>())
            .OrderBy(static condition => condition.Type, StringComparer.Ordinal)
            .ThenBy(static condition => condition.Reason, StringComparer.Ordinal)
            .ToArray();
        Actions = (actions ?? Array.Empty<RuntimeOperatorAction>())
            .OrderBy(static action => action.Id, StringComparer.Ordinal)
            .ToArray();
        var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                orderedMetadata.Add(pair.Key, pair.Value);
            }
        }

        Metadata = orderedMetadata;
    }

    /// <summary>Gets the stable entry identifier within its section.</summary>
    public string Id { get; }

    /// <summary>Gets the operator-facing entry name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the human-readable explanation of the entry.</summary>
    public string Description { get; }

    /// <summary>Gets the state the runtime is expected to maintain.</summary>
    public string DesiredState { get; }

    /// <summary>Gets the state most recently observed by the contributor.</summary>
    public string ObservedState { get; }

    /// <summary>Gets the deterministically ordered operator conditions for the entry.</summary>
    public IReadOnlyList<RuntimeOperatorCondition> Conditions { get; }

    /// <summary>Gets the deterministically ordered actions declared by the owning subsystem.</summary>
    public IReadOnlyList<RuntimeOperatorAction> Actions { get; }

    /// <summary>Gets additional stable metadata for operator tooling.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string RequireValue(string value, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }
}
