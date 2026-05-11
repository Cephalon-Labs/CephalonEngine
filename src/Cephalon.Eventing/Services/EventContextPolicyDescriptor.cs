namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes provider-neutral event context policy metadata for tenant, correlation, causation, baggage, and header propagation.
/// </summary>
/// <remarks>
/// The descriptor is intentionally metadata-only. It lets hosts and modules expose context-policy
/// ownership without putting tenant, correlation, baggage, or header propagation work on the
/// publication or subscription hot path.
/// </remarks>
public sealed class EventContextPolicyDescriptor
{
    /// <summary>
    /// Creates a new event context policy descriptor.
    /// </summary>
    /// <param name="id">The stable context policy identifier.</param>
    /// <param name="displayName">The operator-facing context policy name.</param>
    /// <param name="description">The human-readable context policy description.</param>
    /// <param name="runtimeKind">The runtime implementation kind, such as <c>code-first</c> or <c>provider-managed</c>.</param>
    /// <param name="declaresTenantContext">Whether the policy declares tenant-context propagation metadata.</param>
    /// <param name="declaresCorrelationId">Whether the policy declares correlation-id propagation metadata.</param>
    /// <param name="declaresCausationId">Whether the policy declares causation-id propagation metadata.</param>
    /// <param name="declaresBaggage">Whether the policy declares baggage propagation metadata.</param>
    /// <param name="validatesMessageHeaders">Whether the policy declares message-header validation metadata.</param>
    /// <param name="headerNames">Optional stable message-header names covered by the policy.</param>
    /// <param name="tags">Optional tags that classify the context policy.</param>
    /// <param name="metadata">Optional context policy metadata.</param>
    public EventContextPolicyDescriptor(
        string id,
        string displayName,
        string description,
        string runtimeKind = "code-first",
        bool declaresTenantContext = true,
        bool declaresCorrelationId = true,
        bool declaresCausationId = true,
        bool declaresBaggage = false,
        bool validatesMessageHeaders = false,
        IReadOnlyList<string>? headerNames = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Event context policy id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Event context policy display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Event context policy description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(runtimeKind))
        {
            throw new ArgumentException("Event context policy runtime kind is required.", nameof(runtimeKind));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        RuntimeKind = runtimeKind.Trim();
        DeclaresTenantContext = declaresTenantContext;
        DeclaresCorrelationId = declaresCorrelationId;
        DeclaresCausationId = declaresCausationId;
        DeclaresBaggage = declaresBaggage;
        ValidatesMessageHeaders = validatesMessageHeaders;
        HeaderNames = Normalize(headerNames);
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : metadata
                .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
                .GroupBy(static pair => pair.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.Last().Value?.Trim() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable context policy identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the context policy.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable context policy description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the context policy runtime implementation kind.
    /// </summary>
    public string RuntimeKind { get; }

    /// <summary>
    /// Gets a value indicating whether the policy declares tenant-context propagation metadata.
    /// </summary>
    public bool DeclaresTenantContext { get; }

    /// <summary>
    /// Gets a value indicating whether the policy declares correlation-id propagation metadata.
    /// </summary>
    public bool DeclaresCorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the policy declares causation-id propagation metadata.
    /// </summary>
    public bool DeclaresCausationId { get; }

    /// <summary>
    /// Gets a value indicating whether the policy declares baggage propagation metadata.
    /// </summary>
    public bool DeclaresBaggage { get; }

    /// <summary>
    /// Gets a value indicating whether the policy declares message-header validation metadata.
    /// </summary>
    public bool ValidatesMessageHeaders { get; }

    /// <summary>
    /// Gets the normalized message-header names covered by the policy.
    /// </summary>
    public IReadOnlyList<string> HeaderNames { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the context policy.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets normalized metadata associated with the context policy.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
