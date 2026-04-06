using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Diagnostics;

/// <summary>
/// Structured diagnostic event-id constants for the ABT behavior runtime.
/// IDs are in the range 5100–5109.
/// </summary>
public static class BehaviorDiagnostics
{
    /// <summary>Behavior dispatch started.</summary>
    public static readonly EventId Dispatching = new(5100, nameof(Dispatching));

    /// <summary>Behavior dispatch completed successfully.</summary>
    public static readonly EventId Dispatched = new(5101, nameof(Dispatched));

    /// <summary>Behavior dispatch failed with an exception.</summary>
    public static readonly EventId DispatchFailed = new(5102, nameof(DispatchFailed));

    /// <summary>Behavior compatibility matrix violation detected at startup.</summary>
    public static readonly EventId CompatibilityViolation = new(5103, nameof(CompatibilityViolation));

    /// <summary>Behavior topology resolved from configuration.</summary>
    public static readonly EventId TopologyResolved = new(5104, nameof(TopologyResolved));

    /// <summary>Behavior registered in the catalog.</summary>
    public static readonly EventId BehaviorRegistered = new(5105, nameof(BehaviorRegistered));

    /// <summary>Transport binding initialized for a behavior.</summary>
    public static readonly EventId TransportBound = new(5106, nameof(TransportBound));

    /// <summary>Transport binding failed to initialize.</summary>
    public static readonly EventId TransportBindFailed = new(5107, nameof(TransportBindFailed));

    /// <summary>Advisory contributor added an advisory for a behavior.</summary>
    public static readonly EventId AdvisoryRaised = new(5108, nameof(AdvisoryRaised));

    /// <summary>Behavior execution slot compiled at startup.</summary>
    public static readonly EventId SlotCompiled = new(5109, nameof(SlotCompiled));
}
