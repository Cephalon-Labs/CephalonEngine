using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Exposes the merged set of behavior topology descriptors contributed to the active runtime.
/// Contributors are discovered via DI enumeration over <see cref="IBehaviorContributor" />.
/// </summary>
public sealed class BehaviorCatalog : IBehaviorCatalog
{
    private readonly Dictionary<string, BehaviorTopologyDescriptor> _byId;
    private readonly Dictionary<string, IReadOnlyList<BehaviorTopologyDescriptor>> _byPattern;
    private readonly Dictionary<string, IReadOnlyList<BehaviorTopologyDescriptor>> _byTransport;

    /// <summary>
    /// Initializes a new <see cref="BehaviorCatalog" /> by collecting contributions from all registered contributors.
    /// </summary>
    /// <param name="contributors">The contributors that project behavior topology descriptors.</param>
    public BehaviorCatalog(IEnumerable<IBehaviorContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        All = contributors
            .SelectMany(static c => c.Contribute())
            .GroupBy(static d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static g => g.Last())
            .OrderBy(static d => d.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _byId = All.ToDictionary(static d => d.Id, StringComparer.OrdinalIgnoreCase);

        _byPattern = All
            .GroupBy(static d => d.Pattern, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static g => g.Key,
                static g => (IReadOnlyList<BehaviorTopologyDescriptor>)g
                    .OrderBy(static d => d.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        _byTransport = All
            .SelectMany(static d => d.TransportIds.Select(t => (Transport: t, Descriptor: d)))
            .GroupBy(static x => x.Transport, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static g => g.Key,
                static g => (IReadOnlyList<BehaviorTopologyDescriptor>)g
                    .Select(static x => x.Descriptor)
                    .OrderBy(static d => d.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<BehaviorTopologyDescriptor> All { get; }

    /// <inheritdoc />
    public BehaviorTopologyDescriptor? FindById(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
            return null;
        return _byId.TryGetValue(behaviorId.Trim(), out var match) ? match : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<BehaviorTopologyDescriptor> GetByPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return [];
        return _byPattern.TryGetValue(pattern.Trim(), out var matches) ? matches : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<BehaviorTopologyDescriptor> GetByTransport(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
            return [];
        return _byTransport.TryGetValue(transportId.Trim(), out var matches) ? matches : [];
    }
}
