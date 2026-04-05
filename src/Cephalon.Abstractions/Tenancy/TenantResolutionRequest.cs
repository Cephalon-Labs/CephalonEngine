namespace Cephalon.Abstractions.Tenancy;

/// <summary>
/// Describes the host-neutral hints available when resolving a tenant for the current operation.
/// </summary>
public sealed class TenantResolutionRequest
{
    /// <summary>
    /// Creates a new tenant-resolution request.
    /// </summary>
    /// <param name="hostName">The host name associated with the current request when one is known.</param>
    /// <param name="pathBase">The path base associated with the current request when one is known.</param>
    /// <param name="requestedTenantId">The explicitly requested tenant identifier when one is known.</param>
    /// <param name="requestedTenantKey">The explicitly requested tenant key when one is known.</param>
    /// <param name="userId">The current user identifier when one is known.</param>
    /// <param name="attributes">Optional resolution hints supplied by the host or caller.</param>
    public TenantResolutionRequest(
        string? hostName = null,
        string? pathBase = null,
        string? requestedTenantId = null,
        string? requestedTenantKey = null,
        string? userId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        HostName = string.IsNullOrWhiteSpace(hostName) ? null : hostName.Trim();
        PathBase = string.IsNullOrWhiteSpace(pathBase) ? null : pathBase.Trim();
        RequestedTenantId = string.IsNullOrWhiteSpace(requestedTenantId) ? null : requestedTenantId.Trim();
        RequestedTenantKey = string.IsNullOrWhiteSpace(requestedTenantKey) ? null : requestedTenantKey.Trim();
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        Attributes = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the host name associated with the current request when one is known.
    /// </summary>
    public string? HostName { get; }

    /// <summary>
    /// Gets the path base associated with the current request when one is known.
    /// </summary>
    public string? PathBase { get; }

    /// <summary>
    /// Gets the explicitly requested tenant identifier when one is known.
    /// </summary>
    public string? RequestedTenantId { get; }

    /// <summary>
    /// Gets the explicitly requested tenant key when one is known.
    /// </summary>
    public string? RequestedTenantKey { get; }

    /// <summary>
    /// Gets the current user identifier when one is known.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Gets optional resolution hints supplied by the host or caller.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}
