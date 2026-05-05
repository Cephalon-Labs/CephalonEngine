using System.Runtime.CompilerServices;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Services;
using Neo4j.Driver;

namespace Cephalon.EventSourcing.Neo4j;

/// <summary>
/// Neo4j-backed implementation of <see cref="IEventStore" /> using graph nodes for event streams.
/// Each domain event is stored as an <c>:Event</c> node with a compound node key constraint on
/// <c>(streamId, streamVersion)</c> enforcing optimistic concurrency at the database level.
/// </summary>
public sealed class Neo4jEventStore : IEventStore
{
    private readonly IDriver _driver;
    private readonly string _eventLabel;
    private readonly IEventTypeRegistry _eventTypes;
    private volatile bool _constraintCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jEventStore" /> class.
    /// </summary>
    /// <param name="driver">The Neo4j driver used to open sessions.</param>
    /// <param name="eventLabel">The node label used for event nodes. Defaults to <c>Event</c>.</param>
    public Neo4jEventStore(IDriver driver, string eventLabel = "Event")
        : this(driver, eventLabel, EventTypeRegistry.Empty)
    {
    }

    /// <summary>
    /// Creates a Neo4j event store that uses the supplied event-type registry.
    /// </summary>
    /// <param name="driver">The Neo4j driver used to open sessions.</param>
    /// <param name="eventTypes">The closed event-type registry used to serialize and rehydrate domain events.</param>
    /// <param name="eventLabel">The node label used for event nodes. Defaults to <c>Event</c>.</param>
    /// <returns>A Neo4j event store wired to the supplied event-type registry.</returns>
    public static Neo4jEventStore Create(
        IDriver driver,
        IEventTypeRegistry eventTypes,
        string eventLabel = "Event") =>
        new(driver, eventLabel, eventTypes);

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jEventStore" /> class.
    /// </summary>
    /// <param name="driver">The Neo4j driver used to open sessions.</param>
    /// <param name="eventLabel">The node label used for event nodes. Defaults to <c>Event</c>.</param>
    /// <param name="eventTypes">The closed event-type registry used to serialize and rehydrate domain events.</param>
    internal Neo4jEventStore(
        IDriver driver,
        string eventLabel,
        IEventTypeRegistry eventTypes)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventLabel);
        ArgumentNullException.ThrowIfNull(eventTypes);
        _driver = driver;
        _eventLabel = eventLabel;
        _eventTypes = eventTypes;
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        await EnsureConstraintAsync().ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var label = _eventLabel;
        await using var session = _driver.AsyncSession();
        return await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(
                $"MATCH (e:{label} {{streamId: $streamId}}) RETURN coalesce(max(e.streamVersion), -1) AS version",
                new { streamId = normalizedStreamId }).ConfigureAwait(false);
            var record = await result.SingleAsync().ConfigureAwait(false);
            return record["version"].As<long>();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
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

        await EnsureConstraintAsync().ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow.ToString("O");
        var entries = new List<Neo4jEventEntry>(events.Count);

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

            entries.Add(new Neo4jEventEntry
            {
                StreamId = normalizedStreamId,
                StreamVersion = evt.StreamVersion,
                EventType = _eventTypes.GetName(evt),
                Payload = _eventTypes.Serialize(evt),
                OccurredAtUtc = evt.OccurredAtUtc.ToString("O"),
                AppendedAtUtc = appendedAtUtc
            });
        }

        var label = _eventLabel;
        await using var session = _driver.AsyncSession();
        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                foreach (var entry in entries)
                {
                    var result = await tx.RunAsync(
                        $@"CREATE (e:{label} {{
                            streamId: $streamId,
                            streamVersion: $streamVersion,
                            eventType: $eventType,
                            payload: $payload,
                            occurredAtUtc: $occurredAtUtc,
                            appendedAtUtc: $appendedAtUtc
                        }})",
                        new
                        {
                            streamId = entry.StreamId,
                            streamVersion = entry.StreamVersion,
                            eventType = entry.EventType,
                            payload = entry.Payload,
                            occurredAtUtc = entry.OccurredAtUtc,
                            appendedAtUtc = entry.AppendedAtUtc
                        }).ConfigureAwait(false);
                    _ = await result.ToListAsync().ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }
        catch (ClientException ex) when (ex.Code == "Neo.ClientError.Schema.ConstraintValidationFailed")
        {
            // Constraint violation — optimistic concurrency conflict detected via node key constraint.
            throw new EventStreamConcurrencyException(
                normalizedStreamId,
                expectedVersion,
                await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        await EnsureConstraintAsync().ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var label = _eventLabel;
        await using var session = _driver.AsyncSession();

        var records = await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(
                $@"MATCH (e:{label} {{streamId: $streamId}})
                   WHERE e.streamVersion >= $fromVersion
                   RETURN e.streamId AS streamId, e.streamVersion AS streamVersion,
                          e.eventType AS eventType, e.payload AS payload,
                          e.occurredAtUtc AS occurredAtUtc, e.appendedAtUtc AS appendedAtUtc
                   ORDER BY e.streamVersion ASC",
                new { streamId = normalizedStreamId, fromVersion = fromVersion }).ConfigureAwait(false);
            return await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        foreach (var record in records)
        {
            var entry = new Neo4jEventEntry
            {
                StreamId = record["streamId"].As<string>(),
                StreamVersion = record["streamVersion"].As<long>(),
                EventType = record["eventType"].As<string>(),
                Payload = record["payload"].As<string>(),
                OccurredAtUtc = record["occurredAtUtc"].As<string>(),
                AppendedAtUtc = record["appendedAtUtc"].As<string>()
            };

            yield return _eventTypes.Deserialize(entry.EventType, entry.Payload);
        }
    }

    private async Task EnsureConstraintAsync()
    {
        if (_constraintCreated) return;
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync(Neo4jEventSourcingConfiguration.CreateConstraintCypher).ConfigureAwait(false);
            _ = await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
        _constraintCreated = true;
    }
}
