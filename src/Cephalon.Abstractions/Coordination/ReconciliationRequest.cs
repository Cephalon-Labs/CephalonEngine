namespace Cephalon.Abstractions.Coordination;

/// <summary>Identifies immutable reconciliation intent within a tenant and operation scope.</summary>
/// <remarks>Actor and tenant identifiers are assertions, not authorization grants. A revision must identify the complete immutable intent, including provider payload, in the owning companion.</remarks>
public sealed class ReconciliationRequest
{
    /// <summary>Creates intent. Identifiers are ordinal, case-sensitive, and never normalized.</summary>
    /// <param name="operationId">An idempotency key unique within the tenant.</param>
    /// <param name="tenantId">The explicit tenant scope; use an application-defined scope for non-tenant workloads.</param>
    /// <param name="actorId">The requesting actor assertion, to be verified before applying.</param>
    /// <param name="actionId">The stable action whose implementation is bound by the application.</param>
    /// <param name="targetId">The target resource identity.</param>
    /// <param name="desiredRevision">The immutable desired intent revision.</param>
    /// <param name="expectedRevision">The revision required immediately before mutation.</param>
    public ReconciliationRequest(string operationId, string tenantId, string actorId, string actionId,
        string targetId, string desiredRevision, string expectedRevision)
    {
        OperationId = RequireIdentifier(operationId, nameof(operationId));
        TenantId = RequireIdentifier(tenantId, nameof(tenantId));
        ActorId = RequireIdentifier(actorId, nameof(actorId));
        ActionId = RequireIdentifier(actionId, nameof(actionId));
        TargetId = RequireIdentifier(targetId, nameof(targetId));
        DesiredRevision = RequireIdentifier(desiredRevision, nameof(desiredRevision));
        ExpectedRevision = RequireIdentifier(expectedRevision, nameof(expectedRevision));
    }

    /// <summary>Gets the idempotency key.</summary>
    public string OperationId { get; }
    /// <summary>Gets the tenant scope.</summary>
    public string TenantId { get; }
    /// <summary>Gets the actor assertion.</summary>
    public string ActorId { get; }
    /// <summary>Gets the action identifier.</summary>
    public string ActionId { get; }
    /// <summary>Gets the target resource identity.</summary>
    public string TargetId { get; }
    /// <summary>Gets the desired immutable intent revision.</summary>
    public string DesiredRevision { get; }
    /// <summary>Gets the required pre-mutation revision.</summary>
    public string ExpectedRevision { get; }

    internal static string RequireIdentifier(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Length > 256 || value.Any(char.IsControl))
        {
            throw new ArgumentException("Identifiers must contain at most 256 characters and no control characters.", name);
        }

        return value;
    }
}
