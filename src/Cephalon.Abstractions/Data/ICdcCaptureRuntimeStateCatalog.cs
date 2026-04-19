namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the operator-facing CDC runtime state currently visible for the active runtime.
/// </summary>
public interface ICdcCaptureRuntimeStateCatalog
{
    /// <summary>
    /// Gets the CDC runtime-state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<CdcCaptureRuntimeState> States { get; }

    /// <summary>
    /// Gets one CDC runtime-state entry by its stable capture identifier.
    /// </summary>
    /// <param name="cdcCaptureId">The CDC capture identifier to resolve.</param>
    /// <returns>The matching runtime state, or <see langword="null" /> when that capture is not active.</returns>
    CdcCaptureRuntimeState? GetById(string cdcCaptureId);

    /// <summary>
    /// Gets the CDC runtime-state entries contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching runtime states, or an empty list when the module contributed none.</returns>
    IReadOnlyList<CdcCaptureRuntimeState> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets the CDC runtime-state entries backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching runtime states, or an empty list when the provider contributes none.</returns>
    IReadOnlyList<CdcCaptureRuntimeState> GetByProvider(string provider);

    /// <summary>
    /// Gets the CDC runtime-state entries that publish through the requested outbox.
    /// </summary>
    /// <param name="outboxId">The outbox identifier to filter by.</param>
    /// <returns>The matching runtime states, or an empty list when no capture uses that outbox.</returns>
    IReadOnlyList<CdcCaptureRuntimeState> GetByOutboxId(string outboxId);

    /// <summary>
    /// Gets the CDC runtime-state entries that observe the requested logical source identifier.
    /// </summary>
    /// <param name="sourceId">The source identifier to filter by.</param>
    /// <returns>The matching runtime states, or an empty list when no capture uses that source.</returns>
    IReadOnlyList<CdcCaptureRuntimeState> GetBySourceId(string sourceId);

    /// <summary>
    /// Gets the CDC runtime-state entries that explicitly observe the requested resource identifier.
    /// </summary>
    /// <param name="resourceId">The resource identifier to filter by.</param>
    /// <returns>The matching runtime states, or an empty list when no capture declares that resource.</returns>
    IReadOnlyList<CdcCaptureRuntimeState> GetByResourceId(string resourceId);
}
