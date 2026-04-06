using System.Collections.Frozen;
using Cephalon.Behaviors.Messaging.Abstractions;

namespace Cephalon.Behaviors.Messaging.Registry;

/// <summary>
/// Default implementation of <see cref="IMessagingBehaviorBindingRegistry" />.
/// Uses a <see cref="FrozenDictionary{TKey,TValue}" /> with <see cref="StringComparer.OrdinalIgnoreCase" />
/// for O(1) lock-free transport id lookup.
/// </summary>
public sealed class MessagingBehaviorBindingRegistry : IMessagingBehaviorBindingRegistry
{
    private readonly FrozenDictionary<string, IMessagingBehaviorBinding> _lookup;
    private readonly IReadOnlyList<IMessagingBehaviorBinding> _all;

    /// <summary>
    /// Initializes a new instance of <see cref="MessagingBehaviorBindingRegistry" />
    /// from the supplied set of bindings.
    /// </summary>
    /// <param name="bindings">All registered messaging transport bindings.</param>
    public MessagingBehaviorBindingRegistry(IEnumerable<IMessagingBehaviorBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var list = bindings.ToList();
        _all = list.AsReadOnly();

        // GEN-02: FrozenDictionary with StringComparer.OrdinalIgnoreCase
        _lookup = list.ToFrozenDictionary(
            b => b.TransportId,
            b => b,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IMessagingBehaviorBinding? GetBinding(string transportId)
    {
        ArgumentNullException.ThrowIfNull(transportId);
        return _lookup.TryGetValue(transportId, out var binding) ? binding : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IMessagingBehaviorBinding> All => _all;
}
