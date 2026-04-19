using Cephalon.Abstractions.Execution;

namespace Cephalon.Behaviors.Patterns.Runtime;

internal sealed class SagaChoreographyPublicationRuntimeStateCatalog(
    ISagaChoreographyRuntimeCatalog descriptorCatalog) : ISagaChoreographyPublicationRuntimeStateCatalog, ISagaChoreographyPublicationRuntimeReporter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private readonly Lock gate = new();
    private readonly Dictionary<string, SagaChoreographyRuntimeDescriptor> descriptorsById = descriptorCatalog
        .SagaChoreographies
        .ToDictionary(static descriptor => descriptor.Id, Comparer);
    private readonly Dictionary<string, SagaChoreographyPublicationRuntimeState> statesById = new(Comparer);

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return Order(statesById.Values);
            }
        }
    }

    public SagaChoreographyPublicationRuntimeState? GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        lock (gate)
        {
            return statesById.GetValueOrDefault(id.Trim());
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByBehaviorId(string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        var normalizedBehaviorId = behaviorId.Trim();

        lock (gate)
        {
            return Order(statesById.Values.Where(state => Comparer.Equals(state.BehaviorId, normalizedBehaviorId)));
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetBySourceModule(string sourceModuleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        var normalizedModuleId = sourceModuleId.Trim();

        lock (gate)
        {
            return Order(statesById.Values.Where(state => Comparer.Equals(state.SourceModuleId, normalizedModuleId)));
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByTransportId(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        var normalizedTransportId = transportId.Trim();

        lock (gate)
        {
            return Order(statesById.Values.Where(state => state.TransportIds.Contains(normalizedTransportId, Comparer)));
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return Order(statesById.Values.Where(state => Comparer.Equals(state.ChannelId, normalizedChannelId)));
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();

        lock (gate)
        {
            return Order(statesById.Values.Where(state => Comparer.Equals(state.CorrelationId, normalizedCorrelationId)));
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetCompensationPublications()
    {
        lock (gate)
        {
            return Order(statesById.Values.Where(static state => state.IsCompensation));
        }
    }

    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> GetFailedPublications()
    {
        lock (gate)
        {
            return Order(statesById.Values.Where(static state => state.IsFailed));
        }
    }

    public bool TryGetById(string id, out SagaChoreographyPublicationRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        lock (gate)
        {
            return statesById.TryGetValue(id.Trim(), out state);
        }
    }

    public ValueTask ReportAsync(
        SagaChoreographyPublicationExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        if (!descriptorsById.TryGetValue(report.BehaviorId, out var descriptor))
        {
            throw new InvalidOperationException(
                $"Saga choreography behavior '{report.BehaviorId}' is not registered in the active runtime.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var metadata = report.Metadata.Count == 0
            ? EmptyMetadata
            : new Dictionary<string, string>(report.Metadata, Comparer);

        lock (gate)
        {
            var current = statesById.TryGetValue(report.Id, out var existing)
                ? existing
                : new SagaChoreographyPublicationRuntimeState(
                    Id: report.Id,
                    BehaviorId: descriptor.Id,
                    PublicationId: report.PublicationId,
                    ChannelId: report.ChannelId,
                    EventType: report.EventType,
                    OccurredAtUtc: report.OccurredAtUtc,
                    SourceModuleId: descriptor.SourceModuleId,
                    TransportIds: descriptor.TransportIds,
                    CorrelationId: report.CorrelationId,
                    TenantId: report.TenantId,
                    ContentType: report.ContentType,
                    IsCompensation: report.IsCompensation,
                    LastOutcome: null,
                    LastObservedAtUtc: null,
                    LastPublisherType: null,
                    AcceptedCount: 0,
                    FailedCount: 0,
                    LastError: null,
                    Metadata: EmptyMetadata);

            current = normalizedOutcome switch
            {
                SagaChoreographyPublicationRuntimeOutcomes.Accepted => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastPublisherType = report.PublisherType,
                    AcceptedCount = current.AcceptedCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                SagaChoreographyPublicationRuntimeOutcomes.Failed => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastPublisherType = report.PublisherType,
                    FailedCount = current.FailedCount + 1,
                    LastError = report.Error,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"Saga choreography publication outcome '{report.Outcome}' is not supported by the active runtime.")
            };

            statesById[report.Id] = current;
        }

        return ValueTask.CompletedTask;
    }

    private static SagaChoreographyPublicationRuntimeState[] Order(
        IEnumerable<SagaChoreographyPublicationRuntimeState> states)
    {
        return states
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.SourceModuleId ?? string.Empty, Comparer)
            .ThenBy(static state => state.BehaviorId, Comparer)
            .ThenBy(static state => state.Id, Comparer)
            .ToArray();
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            SagaChoreographyPublicationRuntimeOutcomes.Accepted => SagaChoreographyPublicationRuntimeOutcomes.Accepted,
            SagaChoreographyPublicationRuntimeOutcomes.Failed => SagaChoreographyPublicationRuntimeOutcomes.Failed,
            _ => throw new InvalidOperationException(
                $"Saga choreography publication outcome '{outcome}' is not supported by the active runtime.")
        };
    }
}
