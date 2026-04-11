namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Describes how a behavior-execution exception should participate in resilience handling.
/// </summary>
public enum BehaviorResilienceExceptionHandling
{
    /// <summary>
    /// Ignore the exception for resilience accounting.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Count the exception for circuit-breaker style failure accounting, but do not automatically retry it.
    /// </summary>
    TripOnly = 1,

    /// <summary>
    /// Count the exception for circuit-breaker accounting and treat it as eligible for future retry handling.
    /// </summary>
    RetryAndTrip = 2
}
