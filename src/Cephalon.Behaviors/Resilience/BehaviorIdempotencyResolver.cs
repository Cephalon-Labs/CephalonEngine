using System.Reflection;
using System.Collections.Concurrent;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Resilience;

internal sealed class BehaviorIdempotencyResolver
{
    private readonly IBehaviorTypeRegistry _typeRegistry;
    private readonly ConcurrentDictionary<Type, BehaviorIdempotencyMode> _cache = new();

    public BehaviorIdempotencyResolver(IBehaviorTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(typeRegistry);

        _typeRegistry = typeRegistry;
    }

    public BehaviorIdempotencyMode Resolve(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId) ||
            !_typeRegistry.TryGetType(behaviorId.Trim(), out var behaviorType) ||
            behaviorType is null)
        {
            return BehaviorIdempotencyMode.Unknown;
        }

        return Resolve(behaviorType);
    }

    public BehaviorIdempotencyMode Resolve(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return _cache.GetOrAdd(
            behaviorType,
            static type =>
                type.GetCustomAttribute<BehaviorIdempotencyAttribute>()?.Mode ??
                BehaviorIdempotencyMode.Unknown);
    }
}
