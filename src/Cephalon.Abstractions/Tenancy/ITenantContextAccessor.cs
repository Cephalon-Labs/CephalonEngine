namespace Cephalon.Abstractions.Tenancy;

/// <summary>
/// Exposes the tenant context currently active for the ambient runtime scope.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets the tenant context currently active for the ambient runtime scope.
    /// </summary>
    TenantContext? Current { get; }
}
