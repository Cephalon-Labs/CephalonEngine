using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Represents one behavior execution flowing through the dispatcher pipeline.
/// </summary>
internal sealed record BehaviorExecutionInvocation(
    string BehaviorId,
    Type BehaviorType,
    BehaviorTopologyDescriptor Descriptor,
    object Input,
    IBehaviorContext Context);
