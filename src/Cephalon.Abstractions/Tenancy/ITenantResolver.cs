namespace Cephalon.Abstractions.Tenancy;

/// <summary>
/// Resolves the tenant context for the current operation from host-neutral hints.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant context for the supplied request.
    /// </summary>
    /// <param name="request">The host-neutral resolution request.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the resulting tenant-resolution outcome.</returns>
    ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default);
}
