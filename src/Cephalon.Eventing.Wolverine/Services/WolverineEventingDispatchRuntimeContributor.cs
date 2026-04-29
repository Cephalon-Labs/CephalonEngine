using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineEventingDispatchRuntimeContributor(
    WolverineEventingOptions options,
    IServiceProvider serviceProvider) : IEventDispatchRuntimeContributor
{
    public void RegisterDispatchRuntimes(IEventDispatchRuntimeRegistry dispatchRuntimes)
    {
        ArgumentNullException.ThrowIfNull(dispatchRuntimes);

        if (!options.EnableDispatchLoop)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var outboxIds = scope.ServiceProvider.GetServices<IEventDispatchStore>()
            .SelectMany(static dispatchStore => dispatchStore.OutboxIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static outboxId => outboxId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        dispatchRuntimes.Add(new EventDispatchRuntimeDescriptor(
            id: WolverineEventingRuntimeIds.DispatchRuntimeId,
            displayName: "Wolverine Dispatch Loop",
            description: "Background dispatcher that reads staged Cephalon outbox events and publishes them through Wolverine subscriptions.",
            metadata: new Dictionary<string, string>
            {
                ["adapter"] = "wolverine",
                ["dispatchBridge"] = "wolverine-managed",
                ["dispatchMode"] = "publish-event-publication",
                ["hostWiring"] = options.EnableHostWiring ? "configured" : "external",
                ["publisherId"] = WolverineEventingRuntimeIds.PublisherId,
                ["hostedExecutionId"] = WolverineEventingRuntimeIds.HostedExecutionId,
                ["executionGraphId"] = WolverineEventingRuntimeIds.ExecutionGraphId,
                ["retryPolicy"] = WolverineEventingRetryPolicy.GetDispatchPolicyId(options),
                ["retryMaxAttempts"] = WolverineEventingRetryPolicy.GetDispatchMaxAttempts(options).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["retryDelaySeconds"] = WolverineEventingRetryPolicy.GetDispatchRetryDelaySeconds(options).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["retryDurability"] = "dispatch-store-delayed-eligibility",
                ["retryScope"] = "provider-managed"
            },
            outboxIds: outboxIds));
    }
}
