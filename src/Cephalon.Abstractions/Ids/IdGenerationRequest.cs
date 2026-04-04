namespace Cephalon.Abstractions.Ids;

/// <summary>
/// Describes optional hints supplied to an identifier generator.
/// </summary>
public sealed class IdGenerationRequest
{
    /// <summary>
    /// Creates a new identifier-generation request.
    /// </summary>
    /// <param name="kind">The logical identifier kind or entity category when one is known.</param>
    /// <param name="scope">The logical generation scope when one is known.</param>
    /// <param name="tenantId">The tenant identifier associated with the requested identifier when one is known.</param>
    /// <param name="attributes">Optional generation hints supplied by the caller.</param>
    public IdGenerationRequest(
        string? kind = null,
        string? scope = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        Kind = string.IsNullOrWhiteSpace(kind) ? null : kind.Trim();
        Scope = string.IsNullOrWhiteSpace(scope) ? null : scope.Trim();
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the logical identifier kind or entity category when one is known.
    /// </summary>
    public string? Kind { get; }

    /// <summary>
    /// Gets the logical generation scope when one is known.
    /// </summary>
    public string? Scope { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the requested identifier when one is known.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets optional generation hints supplied by the caller.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets a value indicating whether any generation hints were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Kind is not null ||
        Scope is not null ||
        TenantId is not null ||
        Attributes.Count > 0;
}
