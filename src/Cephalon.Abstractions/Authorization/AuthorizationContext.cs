namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Describes the operation-specific context supplied to an authorization evaluation.
/// </summary>
public sealed class AuthorizationContext
{
    /// <summary>
    /// Creates a new authorization context.
    /// </summary>
    /// <param name="action">The action being requested, such as <c>read</c>, <c>write</c>, or <c>approve</c>.</param>
    /// <param name="policyId">The explicit policy identifier requested by the caller when one is known.</param>
    /// <param name="tenantId">The tenant identifier associated with the current operation.</param>
    /// <param name="correlationId">The correlation identifier associated with the current operation.</param>
    /// <param name="attributes">Optional operation-specific attributes.</param>
    public AuthorizationContext(
        string action,
        string? policyId = null,
        string? tenantId = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Authorization action is required.", nameof(action));
        }

        Action = action.Trim();
        PolicyId = string.IsNullOrWhiteSpace(policyId) ? null : policyId.Trim();
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the action being requested.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets the explicit policy identifier requested by the caller when one is known.
    /// </summary>
    public string? PolicyId { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the current operation.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the current operation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the operation-specific attributes.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}
