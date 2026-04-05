using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Exposes the merged set of event-stream descriptors contributed to the active runtime.
/// </summary>
public sealed class EventStreamCatalog : IEventStoreCatalog
{
    private readonly Dictionary<string, EventStreamDescriptor> descriptorsById;
    private readonly Dictionary<string, IReadOnlyList<EventStreamDescriptor>> descriptorsByProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamCatalog" /> class.
    /// </summary>
    /// <param name="contributors">The contributors that project event-stream descriptors.</param>
    public EventStreamCatalog(IEnumerable<IEventStoreContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        All = contributors
            .SelectMany(static contributor => contributor.Contribute())
            .GroupBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Last())
            .OrderBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        descriptorsById = All.ToDictionary(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase);
        descriptorsByProvider = All
            .GroupBy(static descriptor => descriptor.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventStreamDescriptor>)group
                    .OrderBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all event-stream descriptors contributed to the current runtime.
    /// </summary>
    public IReadOnlyList<EventStreamDescriptor> All { get; }

    /// <summary>
    /// Gets the event-stream descriptors backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching descriptors, or an empty list when the provider contributes none.</returns>
    public IReadOnlyList<EventStreamDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return descriptorsByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }

    /// <summary>
    /// Finds one event-stream descriptor by its stable identifier.
    /// </summary>
    /// <param name="id">The event-stream identifier to resolve.</param>
    /// <returns>The matching descriptor, or <see langword="null" /> when none match.</returns>
    public EventStreamDescriptor? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return descriptorsById.TryGetValue(id.Trim(), out var match)
            ? match
            : null;
    }
}
