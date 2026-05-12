namespace Cephalon.Eventing.Services;

/// <summary>
/// Marks an event dispatch store that can report persisted provider-side Cephalon context proof after applying a dispatch report.
/// </summary>
/// <remarks>
/// Dispatch runtimes should call <see cref="CreatePersistedContextReport" /> only after
/// <see cref="IEventDispatchStore.ApplyReportAsync" /> succeeds. Implementations must not use this contract to claim consumer
/// extraction, downstream delivery completion, or cross-node handoff.
/// </remarks>
public interface IEventDispatchProviderContextPersistenceStore
{
    /// <summary>
    /// Creates a dispatch report copy containing provider-side context-persistence proof for a report the store already applied.
    /// </summary>
    /// <param name="report">The dispatch report that was already applied by the dispatch store.</param>
    /// <returns>A dispatch report enriched with provider-side context-persistence metadata when the original report supports it.</returns>
    EventDispatchExecutionReport CreatePersistedContextReport(EventDispatchExecutionReport report);
}
