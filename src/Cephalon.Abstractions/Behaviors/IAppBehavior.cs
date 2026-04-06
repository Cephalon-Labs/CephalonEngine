namespace Cephalon.Abstractions.Behaviors;

/// <summary>Single interface for all behavior patterns. Developers implement this once; pattern and transport are config-driven.</summary>
public interface IAppBehavior<TIn, TOut>
{
    /// <summary>Handles the behavior input and returns the output.</summary>
    Task<TOut> HandleAsync(TIn input, IBehaviorContext context, CancellationToken ct = default);

    /// <summary>Optional author-intent topology declaration. Called by source generator at build time. Override to declare pattern/transport defaults in code.</summary>
    static virtual void ConfigureTopology(IBehaviorTopologyBuilder builder) { }
}
