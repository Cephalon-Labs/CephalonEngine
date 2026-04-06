using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>Carries all information needed to execute one behavior invocation.</summary>
public sealed class BehaviorExecutionContext
{
    /// <summary>Gets the behavior topology descriptor for this invocation.</summary>
    public required BehaviorTopologyDescriptor Descriptor { get; init; }

    /// <summary>Gets the resolved behavior instance.</summary>
    public required object BehaviorInstance { get; init; }

    /// <summary>Gets the compiled execution slot for invoking the behavior.</summary>
    public required BehaviorExecutionSlot Slot { get; init; }

    /// <summary>Gets the deserialized input object.</summary>
    public required object Input { get; init; }

    /// <summary>Gets the transport-neutral behavior context.</summary>
    public required IBehaviorContext BehaviorContext { get; init; }
}
