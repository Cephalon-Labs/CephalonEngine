using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.EventSourcing.EntityFramework.Services;

internal sealed class EntityFrameworkSnapshotStore<TContext>(TContext dbContext) : ISnapshotStore
    where TContext : DbContext
{
    public async Task SaveSnapshotAsync<TState>(
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

        var normalizedStreamId = streamId.Trim();
        var stateType = CreateStateTypeKey<TState>();
        var payload = JsonSerializer.Serialize(state);
        var snapshots = dbContext.Set<EntityFrameworkEventSnapshotEntry>();
        var existing = await snapshots
            .SingleOrDefaultAsync(
                entry => entry.StreamId == normalizedStreamId && entry.StateType == stateType,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            snapshots.Add(new EntityFrameworkEventSnapshotEntry
            {
                StreamId = normalizedStreamId,
                StateType = stateType,
                StreamVersion = version,
                Payload = payload,
                SavedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            if (version < existing.StreamVersion)
            {
                throw new InvalidOperationException(
                    $"Snapshot for stream '{normalizedStreamId}' already represents version {existing.StreamVersion}, so version {version} cannot replace it.");
            }

            existing.StreamVersion = version;
            existing.Payload = payload;
            existing.SavedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(TState? State, long Version)> LoadSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var normalizedStreamId = streamId.Trim();
        var stateType = CreateStateTypeKey<TState>();
        var snapshot = await dbContext.Set<EntityFrameworkEventSnapshotEntry>()
            .AsNoTracking()
            .Where(entry => entry.StreamId == normalizedStreamId && entry.StateType == stateType)
            .OrderByDescending(entry => entry.StreamVersion)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return (default, -1);
        }

        var state = JsonSerializer.Deserialize<TState>(snapshot.Payload);
        return (state, snapshot.StreamVersion);
    }

    private static string CreateStateTypeKey<TState>()
    {
        var type = typeof(TState);
        var assemblyName = type.Assembly.GetName().Name;
        var typeName = type.FullName ?? type.Name;

        return string.IsNullOrWhiteSpace(assemblyName)
            ? typeName
            : string.Concat(assemblyName, ":", typeName);
    }
}
