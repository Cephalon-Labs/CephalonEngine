namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one active saga-choreography behavior visible to the current runtime.
/// </summary>
/// <remarks>
/// This runtime-facing surface keeps choreography truth derived from the shared behavior topology
/// and registered implementation types instead of inventing a host-only choreography registry. It
/// is intentionally static and operator-facing: it describes the active choreography contract
/// shape, ownership, transports, and publication semantics rather than per-invocation state.
/// </remarks>
public sealed class SagaChoreographyRuntimeDescriptor
{
    /// <summary>
    /// Creates a saga-choreography runtime descriptor.
    /// </summary>
    /// <param name="id">The stable choreography behavior identifier.</param>
    /// <param name="displayName">The operator-facing choreography name.</param>
    /// <param name="description">A human-readable description of the choreography behavior.</param>
    /// <param name="behaviorType">The concrete choreography behavior implementation type name.</param>
    /// <param name="inputType">The choreography input type name.</param>
    /// <param name="resultType">The behavior result-contract type name.</param>
    /// <param name="localOutputType">
    /// The typed local output carried inside the shared choreography result contract when one is
    /// known.
    /// </param>
    /// <param name="sourceModuleId">
    /// The owning module identifier when the choreography came from an explicit module-owned
    /// behavior.
    /// </param>
    /// <param name="transportIds">The transport identifiers that expose the choreography.</param>
    /// <param name="requiredFeatureFlagIds">
    /// The ordered feature-flag identifiers that must resolve to enabled before the choreography can
    /// execute.
    /// </param>
    /// <param name="successStatusCodes">
    /// The HTTP success status codes the shared choreography strategy can return for local output,
    /// publication-only work, or completion without output.
    /// </param>
    /// <param name="metadata">Additional operator-facing metadata describing choreography semantics.</param>
    public SagaChoreographyRuntimeDescriptor(
        string id,
        string displayName,
        string description,
        string behaviorType,
        string inputType,
        string resultType,
        string? localOutputType = null,
        string? sourceModuleId = null,
        IReadOnlyList<string>? transportIds = null,
        IReadOnlyList<string>? requiredFeatureFlagIds = null,
        IReadOnlyList<int>? successStatusCodes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Saga choreography id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Saga choreography display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Saga choreography description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(behaviorType))
        {
            throw new ArgumentException("Saga choreography behavior type is required.", nameof(behaviorType));
        }

        if (string.IsNullOrWhiteSpace(inputType))
        {
            throw new ArgumentException("Saga choreography input type is required.", nameof(inputType));
        }

        if (string.IsNullOrWhiteSpace(resultType))
        {
            throw new ArgumentException("Saga choreography result type is required.", nameof(resultType));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        BehaviorType = behaviorType.Trim();
        InputType = inputType.Trim();
        ResultType = resultType.Trim();
        LocalOutputType = NormalizeOptional(localOutputType);
        SourceModuleId = NormalizeOptional(sourceModuleId);
        TransportIds = NormalizeOrderedStrings(transportIds);
        RequiredFeatureFlagIds = NormalizeOrderedStrings(requiredFeatureFlagIds);
        SuccessStatusCodes = NormalizeStatusCodes(successStatusCodes);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable choreography behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing choreography name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable choreography description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the concrete choreography behavior implementation type name.
    /// </summary>
    public string BehaviorType { get; }

    /// <summary>
    /// Gets the choreography input type name.
    /// </summary>
    public string InputType { get; }

    /// <summary>
    /// Gets the behavior result-contract type name.
    /// </summary>
    public string ResultType { get; }

    /// <summary>
    /// Gets the typed local output carried inside the choreography result contract when one is known.
    /// </summary>
    public string? LocalOutputType { get; }

    /// <summary>
    /// Gets the owning module identifier when one is known at runtime.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets the transport identifiers that expose the choreography.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets the ordered feature-flag identifiers that gate choreography execution.
    /// </summary>
    public IReadOnlyList<string> RequiredFeatureFlagIds { get; }

    /// <summary>
    /// Gets the HTTP success status codes the shared choreography strategy can return.
    /// </summary>
    public IReadOnlyList<int> SuccessStatusCodes { get; }

    /// <summary>
    /// Gets additional operator-facing metadata describing choreography semantics.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string[] NormalizeOrderedStrings(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static int[] NormalizeStatusCodes(IReadOnlyList<int>? statusCodes)
    {
        var normalized = (statusCodes is null || statusCodes.Count == 0
                ? [200, 202, 204]
                : statusCodes)
            .Distinct()
            .OrderBy(static statusCode => statusCode)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one saga choreography success status code is required.", nameof(statusCodes));
        }

        if (normalized.Any(static statusCode => statusCode < 100 || statusCode > 999))
        {
            throw new ArgumentOutOfRangeException(nameof(statusCodes), "Saga choreography status codes must stay in the HTTP status-code range.");
        }

        return normalized;
    }
}
