using Polly;

namespace Cephalon.Behaviors.Resilience;

internal static class BehaviorResilienceExecutionContextKeys
{
    internal static readonly ResiliencePropertyKey<string> BehaviorId =
        new("cephalon.behavior-resilience.behavior-id");

    internal static readonly ResiliencePropertyKey<string> TransportId =
        new("cephalon.behavior-resilience.transport-id");
}
