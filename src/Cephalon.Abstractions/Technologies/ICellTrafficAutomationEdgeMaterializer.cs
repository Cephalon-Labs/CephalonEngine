namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Applies edge-managed cell traffic automation posture to one edge runtime.
/// </summary>
public interface ICellTrafficAutomationEdgeMaterializer
{
    /// <summary>
    /// Gets the stable materializer identifier that should appear on operator-facing runtime answers.
    /// </summary>
    string MaterializerId { get; }

    /// <summary>
    /// Determines whether this materializer owns the requested traffic automation answer.
    /// </summary>
    /// <param name="automation">The effective traffic automation answer to evaluate.</param>
    /// <returns><see langword="true" /> when this materializer should reconcile the automation; otherwise <see langword="false" />.</returns>
    bool CanMaterialize(CellTrafficAutomationRuntimeDescriptor automation);

    /// <summary>
    /// Applies or reconciles the requested traffic automation answer against the owned edge runtime.
    /// </summary>
    /// <param name="automation">The effective traffic automation answer to materialize.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The observed edge-materialization result.</returns>
    ValueTask<CellTrafficAutomationMaterializationResult> MaterializeAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default);
}
