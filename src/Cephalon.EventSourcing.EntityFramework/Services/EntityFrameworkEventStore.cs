using System.Runtime.CompilerServices;
using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.EventSourcing.EntityFramework.Services;

internal sealed class EntityFrameworkEventStore<TContext>(
    TContext dbContext) : IEventStore
    where TContext : DbContext, IEntityFrameworkEventContext
{
    public async Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow;
        foreach (var evt in events)
        {
            ArgumentNullException.ThrowIfNull(evt);

            if (!string.Equals(evt.StreamId, normalizedStreamId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Domain event stream id '{evt.StreamId}' did not match append target '{normalizedStreamId}'.");
            }

            nextVersion++;
            if (evt.StreamVersion != nextVersion)
            {
                throw new InvalidOperationException(
                    $"Domain event '{evt.GetType().FullName}' declared stream version {evt.StreamVersion}, but the append expected version {nextVersion}.");
            }

            dbContext.Events.Add(new EntityFrameworkEventEntry
            {
                StreamId = normalizedStreamId,
                StreamVersion = evt.StreamVersion,
                EventType = evt.GetType().AssemblyQualifiedName
                    ?? throw new InvalidOperationException($"The event type '{evt.GetType().FullName}' must expose an assembly-qualified name."),
                Payload = JsonSerializer.Serialize(evt, evt.GetType()),
                OccurredAtUtc = evt.OccurredAtUtc,
                AppendedAtUtc = appendedAtUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var normalizedStreamId = streamId.Trim();
        var query = dbContext.Events
            .AsNoTracking()
            .Where(entry => entry.StreamId == normalizedStreamId && entry.StreamVersion >= fromVersion)
            .OrderBy(entry => entry.StreamVersion)
            .AsAsyncEnumerable();

        await foreach (var entry in query.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var eventType = Type.GetType(entry.EventType, throwOnError: false);
            if (eventType is null)
            {
                throw new InvalidOperationException(
                    $"The CLR type '{entry.EventType}' could not be resolved while reading stream '{normalizedStreamId}'.");
            }

            var evt = JsonSerializer.Deserialize(entry.Payload, eventType) as IDomainEvent;
            if (evt is null)
            {
                throw new InvalidOperationException(
                    $"The payload for event type '{entry.EventType}' in stream '{normalizedStreamId}' could not be deserialized as an IDomainEvent.");
            }

            yield return evt;
        }
    }

    public async Task<long> GetVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var normalizedStreamId = streamId.Trim();
        return await dbContext.Events
            .Where(entry => entry.StreamId == normalizedStreamId)
            .Select(static entry => (long?)entry.StreamVersion)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? -1;
    }
}
