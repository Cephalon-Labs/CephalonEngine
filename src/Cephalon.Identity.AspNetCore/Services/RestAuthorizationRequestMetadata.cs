namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class RestAuthorizationRequestMetadata
{
    public RestAuthorizationRequestMetadata(
        string policyId,
        string? action,
        string? resourceType,
        string? resourceIdRouteKey,
        string? tenantRouteKey,
        string? ownerSubjectIdRouteKey)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            throw new ArgumentException("Authorization policy id is required.", nameof(policyId));
        }

        PolicyId = policyId.Trim();
        Action = Normalize(action);
        ResourceType = Normalize(resourceType);
        ResourceIdRouteKey = Normalize(resourceIdRouteKey);
        TenantRouteKey = Normalize(tenantRouteKey);
        OwnerSubjectIdRouteKey = Normalize(ownerSubjectIdRouteKey);
    }

    public string PolicyId { get; }

    public string? Action { get; }

    public string? ResourceType { get; }

    public string? ResourceIdRouteKey { get; }

    public string? TenantRouteKey { get; }

    public string? OwnerSubjectIdRouteKey { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
