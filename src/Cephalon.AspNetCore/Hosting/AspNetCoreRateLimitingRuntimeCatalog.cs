using Cephalon.Abstractions.Resilience;
namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreRateLimitingRuntimeCatalog : IRateLimitingRuntimeCatalog
{
    private readonly RateLimitingRuntimeDescriptor[] policies;
    private readonly Dictionary<string, RateLimitingRuntimeDescriptor> policiesById;
    private readonly Dictionary<string, IReadOnlyList<RateLimitingRuntimeDescriptor>> policiesByTransportId;

    public AspNetCoreRateLimitingRuntimeCatalog(AspNetCoreRateLimitingPolicyCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        policies = catalog.EnabledPolicies
            .Select(static policy => policy.ToDescriptor())
            .ToArray();
        if (policies.Length == 0)
        {
            policiesById = new Dictionary<string, RateLimitingRuntimeDescriptor>(StringComparer.OrdinalIgnoreCase);
            policiesByTransportId = new Dictionary<string, IReadOnlyList<RateLimitingRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        policiesById = policies.ToDictionary(static policy => policy.Id, StringComparer.OrdinalIgnoreCase);
        policiesByTransportId = policies
            .SelectMany(static policy => policy.TransportIds.Select(transportId => new KeyValuePair<string, RateLimitingRuntimeDescriptor>(transportId, policy)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RateLimitingRuntimeDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RateLimitingRuntimeDescriptor> Policies => policies;

    public RateLimitingRuntimeDescriptor? GetById(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        return policiesById.TryGetValue(policyId.Trim(), out var policy)
            ? policy
            : null;
    }

    public IReadOnlyList<RateLimitingRuntimeDescriptor> GetByTransportId(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
        {
            return [];
        }

        var lookupKey = AspNetCoreRateLimitingPolicyResolver.CanonicalizeTransportId(transportId) ?? transportId.Trim();
        return policiesByTransportId.TryGetValue(lookupKey, out var matches)
            ? matches
            : [];
    }
}
