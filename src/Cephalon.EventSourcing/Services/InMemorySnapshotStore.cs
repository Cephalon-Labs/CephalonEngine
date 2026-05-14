using Cephalon.Abstractions.EventSourcing;
using System.Collections.Concurrent;

namespace Cephalon.EventSourcing.Services;

internal sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<string, SnapshotEntry> snapshots = new(StringComparer.Ordinal);

    public Task SaveSnapshotAsync<TState>(
        string streamId,
        long version,
        TState state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Snapshot version cannot be negative.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        snapshots[CreateKey<TState>(streamId)] = new SnapshotEntry(version, state);
        return Task.CompletedTask;
    }

    public Task<(TState? State, long Version)> LoadSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (snapshots.TryGetValue(CreateKey<TState>(streamId), out var snapshot) &&
            snapshot.State is TState state)
        {
            return Task.FromResult< (TState? State, long Version)>((state, snapshot.Version));
        }

        return Task.FromResult< (TState? State, long Version)>((default, -1));
    }

    private static string CreateKey<TState>(string streamId)
    {
        return $"{typeof(TState).AssemblyQualifiedName}\u001F{streamId.Trim()}";
    }

    private sealed record SnapshotEntry(long Version, object? State);
}
