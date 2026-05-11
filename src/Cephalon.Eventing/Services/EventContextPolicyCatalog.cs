using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventContextPolicyCatalog : IEventContextPolicyCatalog
{
    private readonly Dictionary<string, EventContextPolicyDescriptor> byId;
    private readonly Dictionary<string, IReadOnlyList<EventContextPolicyDescriptor>> byHeaderName;

    public EventContextPolicyCatalog(
        EventingOptions options,
        IEnumerable<IEventContextPolicyContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventContextPolicyRegistry();
        foreach (var policy in options.ContextPolicies)
        {
            registry.Add(policy);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterEventContextPolicies(registry);
        }

        Policies = registry.Build();
        byId = Policies.ToDictionary(static policy => policy.Id, StringComparer.OrdinalIgnoreCase);
        byHeaderName = Policies
            .SelectMany(static policy => policy.HeaderNames.Select(headerName => (HeaderName: headerName, Policy: policy)))
            .GroupBy(static item => item.HeaderName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventContextPolicyDescriptor>)group
                    .Select(static item => item.Policy)
                    .DistinctBy(static policy => policy.Id, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static policy => policy.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventContextPolicyDescriptor> Policies { get; }

    public IReadOnlyList<EventContextPolicyDescriptor> GetByHeaderName(string headerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        return byHeaderName.TryGetValue(headerName.Trim(), out var policies)
            ? policies
            : [];
    }

    public bool TryGet(string policyId, out EventContextPolicyDescriptor policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        return byId.TryGetValue(policyId.Trim(), out policy!);
    }
}
