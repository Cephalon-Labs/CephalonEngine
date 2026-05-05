namespace Cephalon.Behaviors.Services;

internal sealed class BehaviorExecutionSlotRegistry
{
    private readonly Dictionary<string, (Type BehaviorType, BehaviorExecutionSlot Slot)> _map =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(string behaviorId, Type behaviorType, BehaviorExecutionSlot slot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(slot);

        if (_map.TryGetValue(behaviorId, out var existing) &&
            existing.BehaviorType != behaviorType)
        {
            throw new InvalidOperationException(
                $"Cannot register execution slot for behavior id '{behaviorId}' and type '{behaviorType.FullName}' because it is already registered by '{existing.BehaviorType.FullName}'.");
        }

        _map[behaviorId] = (behaviorType, slot);
    }

    public bool TryGetSlot(
        string behaviorId,
        Type behaviorType,
        out BehaviorExecutionSlot? slot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (_map.TryGetValue(behaviorId, out var entry) &&
            entry.BehaviorType == behaviorType)
        {
            slot = entry.Slot;
            return true;
        }

        slot = null;
        return false;
    }
}
