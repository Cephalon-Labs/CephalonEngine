namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Accepts live cell traffic automation materialization observations that should merge back into the active runtime catalog.
/// </summary>
public interface ICellTrafficAutomationMaterializationReportSink
{
    /// <summary>
    /// Reports a provider-managed materialization observation for one traffic automation answer.
    /// </summary>
    /// <param name="automationId">The stable traffic-automation identifier that the observation applies to.</param>
    /// <param name="materializerId">The stable provider materializer identifier that produced the observation.</param>
    /// <param name="result">The observed provider-managed materialization result.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    ValueTask ReportProviderAsync(
        string automationId,
        string materializerId,
        CellTrafficAutomationProviderMaterializationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports an edge-managed materialization observation for one traffic automation answer.
    /// </summary>
    /// <param name="automationId">The stable traffic-automation identifier that the observation applies to.</param>
    /// <param name="materializerId">The stable edge materializer identifier that produced the observation.</param>
    /// <param name="result">The observed edge-managed materialization result.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    ValueTask ReportEdgeAsync(
        string automationId,
        string materializerId,
        CellTrafficAutomationMaterializationResult result,
        CancellationToken cancellationToken = default);
}
