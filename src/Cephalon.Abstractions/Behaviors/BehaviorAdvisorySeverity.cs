namespace Cephalon.Abstractions.Behaviors;

/// <summary>Severity levels for behavior advisories.</summary>
public enum BehaviorAdvisorySeverity
{
    /// <summary>Informational — no action required.</summary>
    Info = 0,

    /// <summary>Warning — review recommended.</summary>
    Warning = 1,

    /// <summary>Critical — immediate attention recommended.</summary>
    Critical = 2
}
