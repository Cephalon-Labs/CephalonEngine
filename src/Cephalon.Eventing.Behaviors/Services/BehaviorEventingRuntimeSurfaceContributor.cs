using Cephalon.Abstractions.Technologies;

namespace Cephalon.Eventing.Behaviors.Services;

internal sealed class BehaviorEventingRuntimeSurfaceContributor : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "saga-choreography-bridges",
            displayName: "Saga Choreography Bridges",
            description: "Explicit bridges that route saga choreography publications into event-driven integration runtimes.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "eventing-publish-bridge",
                    displayName: "Eventing Publish Bridge",
                    description: "Stages saga choreography publications through the shared Cephalon.Eventing publish path.",
                    metadata: new Dictionary<string, string>
                    {
                        ["pattern"] = "saga-choreography",
                        ["activation"] = "explicit",
                        ["handoff"] = "eventing.publish",
                        ["durabilityPrerequisite"] = "outbox",
                        ["ownership"] = "engine-managed"
                    })
            ]);
    }
}
