namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the CDC capture surfaces visible to the current runtime.
/// </summary>
public interface ICdcCaptureCatalog
{
    /// <summary>
    /// Gets all CDC capture surfaces visible to the current runtime.
    /// </summary>
    IReadOnlyList<CdcCaptureDescriptor> CdcCaptures { get; }

    /// <summary>
    /// Gets one CDC capture by its stable identifier.
    /// </summary>
    /// <param name="cdcCaptureId">The CDC capture identifier to resolve.</param>
    /// <returns>The matching CDC capture, or <see langword="null" /> when it is not active.</returns>
    CdcCaptureDescriptor? GetById(string cdcCaptureId);

    /// <summary>
    /// Gets all CDC captures contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching CDC captures, or an empty list when the module contributed none.</returns>
    IReadOnlyList<CdcCaptureDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all CDC captures backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching CDC captures, or an empty list when the provider contributes none.</returns>
    IReadOnlyList<CdcCaptureDescriptor> GetByProvider(string provider);

    /// <summary>
    /// Gets all CDC captures that publish through the requested outbox.
    /// </summary>
    /// <param name="outboxId">The outbox identifier to filter by.</param>
    /// <returns>The matching CDC captures, or an empty list when no capture uses that outbox.</returns>
    IReadOnlyList<CdcCaptureDescriptor> GetByOutboxId(string outboxId);

    /// <summary>
    /// Gets all CDC captures that observe the requested logical source identifier.
    /// </summary>
    /// <param name="sourceId">The source identifier to filter by.</param>
    /// <returns>The matching CDC captures, or an empty list when no capture uses that source.</returns>
    IReadOnlyList<CdcCaptureDescriptor> GetBySourceId(string sourceId);

    /// <summary>
    /// Gets all CDC captures that explicitly observe the requested resource identifier.
    /// </summary>
    /// <param name="resourceId">The resource identifier to filter by.</param>
    /// <returns>The matching CDC captures, or an empty list when no capture declares that resource.</returns>
    IReadOnlyList<CdcCaptureDescriptor> GetByResourceId(string resourceId);
}
