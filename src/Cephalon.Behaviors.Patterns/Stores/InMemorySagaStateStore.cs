using System.Collections.Concurrent;
using System.Text.Json;
using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Stores;

/// <summary>
/// An in-memory implementation of <see cref="ISagaStateStore"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> with JSON serialization.
/// Suitable for development and testing; replace with a durable store for production use.
/// </summary>
public sealed class InMemorySagaStateStore : ISagaStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptions.Default;

    private readonly ConcurrentDictionary<string, string> _store =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves and deserializes the saga state for the given saga identifier.
    /// </summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="sagaId">The unique identifier of the saga instance.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>The deserialized state, or <see langword="null"/> if not found.</returns>
    public Task<T?> GetAsync<T>(string sagaId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);

        if (_store.TryGetValue(sagaId, out var json))
        {
            var value = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            return Task.FromResult(value);
        }

        return Task.FromResult<T?>(default);
    }

    /// <summary>
    /// Serializes and upserts the saga state for the given saga identifier.
    /// </summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="sagaId">The unique identifier of the saga instance.</param>
    /// <param name="state">The state to persist.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A completed task.</returns>
    public Task SaveAsync<T>(string sagaId, T state, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);
        ArgumentNullException.ThrowIfNull(state);

        var json = JsonSerializer.Serialize(state, SerializerOptions);
        _store[sagaId] = json;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the saga state for the given saga identifier.
    /// </summary>
    /// <param name="sagaId">The unique identifier of the saga instance to remove.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A completed task.</returns>
    public Task DeleteAsync(string sagaId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);

        _store.TryRemove(sagaId, out _);
        return Task.CompletedTask;
    }
}
