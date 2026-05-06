using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorIdempotencyResolver
{
    private readonly Dictionary<string, BehaviorIdempotencyMode> byBehaviorId;
    private readonly Dictionary<Type, BehaviorIdempotencyMode> byBehaviorType;

    public BehaviorIdempotencyResolver(IEnumerable<BehaviorImplementationDescriptor> implementations)
    {
        ArgumentNullException.ThrowIfNull(implementations);

        byBehaviorId = new Dictionary<string, BehaviorIdempotencyMode>(StringComparer.OrdinalIgnoreCase);
        byBehaviorType = [];
        foreach (var implementation in implementations)
        {
            ArgumentNullException.ThrowIfNull(implementation);
            byBehaviorId[implementation.Id] = implementation.IdempotencyMode;
            byBehaviorType[implementation.BehaviorType] = implementation.IdempotencyMode;
        }
    }

    public BehaviorIdempotencyMode Resolve(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return BehaviorIdempotencyMode.Unknown;
        }

        return byBehaviorId.TryGetValue(behaviorId.Trim(), out var idempotencyMode)
            ? idempotencyMode
            : BehaviorIdempotencyMode.Unknown;
    }

    public BehaviorIdempotencyMode Resolve(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return byBehaviorType.TryGetValue(behaviorType, out var idempotencyMode)
            ? idempotencyMode
            : BehaviorIdempotencyMode.Unknown;
    }
}
