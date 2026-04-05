namespace Cephalon.Abstractions.Tenancy;

/// <summary>
/// Describes the outcome of one tenant-resolution attempt.
/// </summary>
public sealed class TenantResolutionResult
{
    /// <summary>
    /// Creates a new tenant-resolution result.
    /// </summary>
    /// <param name="tenant">The resolved tenant context when resolution succeeded.</param>
    /// <param name="source">The source or strategy that produced the result when one is known.</param>
    /// <param name="reason">The human-readable reason associated with the result.</param>
    public TenantResolutionResult(
        TenantContext? tenant,
        string? source = null,
        string? reason = null)
    {
        Tenant = tenant;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>
    /// Gets the resolved tenant context when resolution succeeded.
    /// </summary>
    public TenantContext? Tenant { get; }

    /// <summary>
    /// Gets the source or strategy that produced the result when one is known.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the human-readable reason associated with the result.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets a value indicating whether tenant resolution succeeded.
    /// </summary>
    public bool IsResolved => Tenant is not null;
}
