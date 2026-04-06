using System.Collections.Frozen;
using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Registry;

/// <summary>
/// Provides O(1) lookup of <see cref="IBehaviorExecutionStrategy"/> instances by pattern identifier.
/// The registry is built once at construction time from a frozen dictionary for lock-free reads.
/// </summary>
public sealed class ExecutionStrategyRegistry
{
    private readonly FrozenDictionary<string, IBehaviorExecutionStrategy> _strategies;

    /// <summary>
    /// Initializes the registry from the provided strategies.
    /// Duplicate pattern identifiers (case-insensitive) are not allowed.
    /// </summary>
    /// <param name="strategies">The collection of strategies to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategies"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when two strategies share the same pattern identifier.</exception>
    public ExecutionStrategyRegistry(IEnumerable<IBehaviorExecutionStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        _strategies = strategies
            .ToDictionary(s => s.Pattern, s => s, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets all registered strategies.</summary>
    public IReadOnlyList<IBehaviorExecutionStrategy> All => _strategies.Values.ToList();

    /// <summary>
    /// Retrieves the strategy for the given pattern identifier.
    /// </summary>
    /// <param name="pattern">The pattern identifier to look up (case-insensitive).</param>
    /// <returns>The matching strategy, or <see langword="null"/> if not found.</returns>
    public IBehaviorExecutionStrategy? GetStrategy(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return _strategies.TryGetValue(pattern, out var strategy) ? strategy : null;
    }
}
