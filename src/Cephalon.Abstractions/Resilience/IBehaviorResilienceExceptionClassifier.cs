namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Classifies behavior-execution exceptions for resilience handling.
/// </summary>
/// <remarks>
/// This contract lets hosts or companion packs decide which failures should count toward
/// circuit-breaker style failure accounting and which ones should stay outside resilience
/// automation because they represent business or validation outcomes.
/// </remarks>
public interface IBehaviorResilienceExceptionClassifier
{
    /// <summary>
    /// Classifies one behavior-execution exception.
    /// </summary>
    /// <param name="context">The exception context being evaluated.</param>
    /// <returns>The resilience-handling mode that should apply.</returns>
    BehaviorResilienceExceptionHandling Classify(BehaviorResilienceExceptionContext context);
}
