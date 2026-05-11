namespace Cephalon.Eventing.Services;

internal sealed class EventContextPolicyRegistry : IEventContextPolicyRegistry
{
    private readonly List<EventContextPolicyDescriptor> policies = [];

    public void Add(EventContextPolicyDescriptor policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policies.Add(policy);
    }

    public IReadOnlyList<EventContextPolicyDescriptor> Build()
    {
        return policies
            .GroupBy(static policy => policy.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .OrderBy(static policy => policy.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
