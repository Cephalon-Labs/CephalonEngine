using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Configuration;

namespace Cephalon.EventSourcing.Services;

internal sealed class EventStreamReplayWorker(
    EventSourcingOptions options,
    IEnumerable<ISnapshotStore> snapshotStores,
    IEnumerable<IProjection<IDomainEvent>> projections,
    EventStreamReplayRuntimeState runtimeState) : IEventStreamReplayWorker
{
    private readonly ISnapshotStore? snapshotStore = snapshotStores.LastOrDefault();
    private readonly IProjection<IDomainEvent>[] projections = projections.ToArray();

    public async Task<EventStreamReplayResult<TState>> ReplayAggregateAsync<TAggregate, TState>(
        IEventStore eventStore,
        EventStreamReplayRequest request,
        CancellationToken cancellationToken = default)
        where TAggregate : IAggregate<TState>, new()
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(request);

        if (!options.EnableReplayWorker)
        {
            throw new InvalidOperationException("The Cephalon event-stream replay worker is disabled for this host.");
        }

        var startedAtUtc = DateTimeOffset.UtcNow;
        var aggregate = new TAggregate();
        var state = default(TState)!;
        var snapshotVersion = -1L;
        var replayFromVersion = request.FromVersion;
        var lastReplayedVersion = request.FromVersion <= 0 ? -1 : request.FromVersion - 1;
        var usedSnapshot = false;
        var snapshotSaved = false;
        var replayedEventCount = 0;
        var projectedEventCount = 0;
        var projectionCount = request.RebuildProjections ? projections.Length : 0;

        try
        {
            if (options.EnableSnapshots &&
                request.UseSnapshots &&
                snapshotStore is not null)
            {
                var snapshot = await snapshotStore.LoadSnapshotAsync<TState>(request.StreamId, cancellationToken)
                    .ConfigureAwait(false);

                if (snapshot.Version >= request.FromVersion && snapshot.State is not null)
                {
                    state = snapshot.State;
                    snapshotVersion = snapshot.Version;
                    lastReplayedVersion = snapshot.Version;
                    replayFromVersion = snapshot.Version + 1;
                    usedSnapshot = true;
                }
            }

            await foreach (var evt in eventStore.ReadStreamAsync(request.StreamId, replayFromVersion, cancellationToken)
                .ConfigureAwait(false))
            {
                state = aggregate.Apply(state, evt);
                lastReplayedVersion = evt.StreamVersion;
                replayedEventCount++;

                if (request.RebuildProjections)
                {
                    foreach (var projection in projections)
                    {
                        await projection.ProjectAsync(evt, cancellationToken).ConfigureAwait(false);
                        projectedEventCount++;
                    }
                }
            }

            if (options.EnableSnapshots &&
                request.SaveSnapshot &&
                snapshotStore is not null &&
                lastReplayedVersion >= 0)
            {
                await snapshotStore.SaveSnapshotAsync(request.StreamId, lastReplayedVersion, state, cancellationToken)
                    .ConfigureAwait(false);
                snapshotSaved = true;
            }

            var completedReport = CreateReport(
                request.StreamId,
                "passed",
                startedAtUtc,
                usedSnapshot,
                snapshotVersion,
                replayFromVersion,
                replayedEventCount,
                projectionCount,
                projectedEventCount,
                lastReplayedVersion,
                snapshotSaved);

            runtimeState.Record(completedReport);
            return new EventStreamReplayResult<TState>(state, completedReport);
        }
        catch (Exception exception)
        {
            runtimeState.Record(CreateReport(
                request.StreamId,
                "failed",
                startedAtUtc,
                usedSnapshot,
                snapshotVersion,
                replayFromVersion,
                replayedEventCount,
                projectionCount,
                projectedEventCount,
                lastReplayedVersion,
                snapshotSaved,
                exception.Message));

            throw;
        }
    }

    private static EventStreamReplayReport CreateReport(
        string streamId,
        string status,
        DateTimeOffset startedAtUtc,
        bool usedSnapshot,
        long snapshotVersion,
        long replayFromVersion,
        int replayedEventCount,
        int projectionCount,
        int projectedEventCount,
        long lastReplayedVersion,
        bool snapshotSaved,
        string? error = null)
    {
        return new EventStreamReplayReport(
            streamId,
            status,
            startedAtUtc,
            DateTimeOffset.UtcNow,
            usedSnapshot,
            snapshotVersion,
            replayFromVersion,
            replayedEventCount,
            projectionCount,
            projectedEventCount,
            lastReplayedVersion,
            snapshotSaved,
            error);
    }
}
