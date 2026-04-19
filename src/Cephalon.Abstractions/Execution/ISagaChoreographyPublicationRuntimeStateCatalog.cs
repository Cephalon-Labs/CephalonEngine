namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Exposes the operator-facing live saga-choreography publication state currently reported for the
/// active runtime.
/// </summary>
public interface ISagaChoreographyPublicationRuntimeStateCatalog
{
    /// <summary>
    /// Gets the reported choreography publication-state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> States { get; }

    /// <summary>
    /// Gets the latest reported publication state for one choreography publication path.
    /// </summary>
    /// <param name="id">The stable runtime-state identifier to resolve.</param>
    /// <returns>
    /// The latest reported publication state, or <see langword="null" /> when that identifier has
    /// not reported choreography runtime state.
    /// </returns>
    SagaChoreographyPublicationRuntimeState? GetById(string id);

    /// <summary>
    /// Gets the reported publication-state entries for one choreography behavior.
    /// </summary>
    /// <param name="behaviorId">The stable choreography behavior identifier to filter by.</param>
    /// <returns>
    /// The matching publication-state entries, or an empty list when the behavior has not reported
    /// live publication state.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByBehaviorId(string behaviorId);

    /// <summary>
    /// Gets the reported publication-state entries contributed by one source module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>
    /// The matching publication-state entries, or an empty list when the module has not reported
    /// live publication state.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets the reported publication-state entries exposed over one transport.
    /// </summary>
    /// <param name="transportId">The stable transport identifier to filter by.</param>
    /// <returns>
    /// The matching publication-state entries, or an empty list when none reported live publication
    /// state for that transport.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByTransportId(string transportId);

    /// <summary>
    /// Gets the reported publication-state entries that targeted the requested channel.
    /// </summary>
    /// <param name="channelId">The logical channel identifier to filter by.</param>
    /// <returns>
    /// The matching publication-state entries, or an empty list when no publication targeted that
    /// channel.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByChannelId(string channelId);

    /// <summary>
    /// Gets the reported publication-state entries associated with one correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier to filter by.</param>
    /// <returns>
    /// The matching publication-state entries, or an empty list when none reported that correlation.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByCorrelationId(string correlationId);

    /// <summary>
    /// Gets the reported publication-state entries that currently represent compensation work.
    /// </summary>
    /// <returns>
    /// The matching compensation publication-state entries, or an empty list when none reported
    /// compensation posture.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetCompensationPublications();

    /// <summary>
    /// Gets the reported publication-state entries whose latest observation is failed.
    /// </summary>
    /// <returns>
    /// The matching failed publication-state entries, or an empty list when none currently report a
    /// failed posture.
    /// </returns>
    IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetFailedPublications();

    /// <summary>
    /// Tries to get the latest reported publication state for one choreography publication path.
    /// </summary>
    /// <param name="id">The stable runtime-state identifier to resolve.</param>
    /// <param name="state">
    /// Receives the latest reported publication state when that identifier has reported one.
    /// </param>
    /// <returns><see langword="true" /> when a reported state exists; otherwise, <see langword="false" />.</returns>
    bool TryGetById(string id, out SagaChoreographyPublicationRuntimeState? state);
}
