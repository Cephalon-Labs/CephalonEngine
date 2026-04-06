using System.Collections.Frozen;
using Cephalon.Behaviors.Http.Abstractions;

namespace Cephalon.Behaviors.Http.Registry;

/// <summary>
/// Default implementation of <see cref="IHttpBehaviorBindingRegistry" />.
/// Bindings are indexed at construction time using a frozen dictionary for
/// O(1) lock-free lookup on every request.
/// </summary>
public sealed class HttpBehaviorBindingRegistry : IHttpBehaviorBindingRegistry
{
    private readonly FrozenDictionary<string, IHttpBehaviorBinding> _index;
    private readonly IReadOnlyList<IHttpBehaviorBinding> _all;

    /// <summary>
    /// Initializes the registry from the supplied enumeration of bindings.
    /// Duplicate transport IDs are not allowed; the last registration wins.
    /// </summary>
    /// <param name="bindings">The bindings to register.</param>
    public HttpBehaviorBindingRegistry(IEnumerable<IHttpBehaviorBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var list = bindings.ToList();
        _all = list.AsReadOnly();
        _index = list.ToFrozenDictionary(
            b => b.TransportId,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IHttpBehaviorBinding? GetBinding(string transportId)
    {
        ArgumentNullException.ThrowIfNull(transportId);
        return _index.TryGetValue(transportId, out var binding) ? binding : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IHttpBehaviorBinding> All => _all;
}
