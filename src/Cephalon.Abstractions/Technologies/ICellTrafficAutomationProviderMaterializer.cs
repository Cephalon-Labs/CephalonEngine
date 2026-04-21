namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Applies provider-managed cell traffic automation posture to one external control plane.
/// </summary>
public interface ICellTrafficAutomationProviderMaterializer
{
    /// <summary>
    /// Gets the stable materializer identifier that should appear on operator-facing runtime answers.
    /// </summary>
    string MaterializerId { get; }

    /// <summary>
    /// Gets the external provider identifier that this materializer reconciles.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Applies or reconciles the requested traffic automation answer against the owned provider.
    /// </summary>
    /// <param name="automation">The effective traffic automation answer to materialize.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The observed provider-materialization result.</returns>
    ValueTask<CellTrafficAutomationProviderMaterializationResult> MaterializeAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default);
}
