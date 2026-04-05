namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Describes the outcome of one authorization evaluation.
/// </summary>
public sealed class AuthorizationDecision
{
    /// <summary>
    /// Creates a new authorization decision.
    /// </summary>
    /// <param name="isAllowed">Whether access was allowed.</param>
    /// <param name="policyId">The policy identifier that produced the decision when one is known.</param>
    /// <param name="reason">The human-readable reason associated with the decision.</param>
    /// <param name="modes">The authorization modes that participated in the decision.</param>
    /// <param name="metadata">Optional decision metadata.</param>
    public AuthorizationDecision(
        bool isAllowed,
        string? policyId = null,
        string? reason = null,
        IReadOnlyList<AuthorizationMode>? modes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        IsAllowed = isAllowed;
        PolicyId = string.IsNullOrWhiteSpace(policyId) ? null : policyId.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Modes = modes?
            .Distinct()
            .OrderBy(static mode => mode)
            .ToArray() ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a value indicating whether access was allowed.
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// Gets the policy identifier that produced the decision when one is known.
    /// </summary>
    public string? PolicyId { get; }

    /// <summary>
    /// Gets the human-readable reason associated with the decision.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the authorization modes that participated in the decision.
    /// </summary>
    public IReadOnlyList<AuthorizationMode> Modes { get; }

    /// <summary>
    /// Gets optional decision metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates an allowed authorization decision.
    /// </summary>
    /// <param name="policyId">The policy identifier that produced the decision when one is known.</param>
    /// <param name="reason">The human-readable reason associated with the decision.</param>
    /// <param name="modes">The authorization modes that participated in the decision.</param>
    /// <param name="metadata">Optional decision metadata.</param>
    /// <returns>An allowed authorization decision.</returns>
    public static AuthorizationDecision Allow(
        string? policyId = null,
        string? reason = null,
        IReadOnlyList<AuthorizationMode>? modes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AuthorizationDecision(true, policyId, reason, modes, metadata);
    }

    /// <summary>
    /// Creates a denied authorization decision.
    /// </summary>
    /// <param name="policyId">The policy identifier that produced the decision when one is known.</param>
    /// <param name="reason">The human-readable reason associated with the decision.</param>
    /// <param name="modes">The authorization modes that participated in the decision.</param>
    /// <param name="metadata">Optional decision metadata.</param>
    /// <returns>A denied authorization decision.</returns>
    public static AuthorizationDecision Deny(
        string? policyId = null,
        string? reason = null,
        IReadOnlyList<AuthorizationMode>? modes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AuthorizationDecision(false, policyId, reason, modes, metadata);
    }
}
