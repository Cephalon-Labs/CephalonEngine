using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventPublicationScheduleQueue(
    EventingOptions options,
    IEventChannelCatalog channels,
    IEventPublicationRuntimeReporter publicationRuntimeReporter,
    IServiceScopeFactory scopeFactory) : IAsyncDisposable
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, ScheduledPublication> pending = new(StringComparer.OrdinalIgnoreCase);
    private Timer? timer;
    private int dispatching;
    private bool disposed;

    public int PendingCount
    {
        get
        {
            lock (gate)
            {
                return pending.Count;
            }
        }
    }

    public DateTimeOffset? NextDueAtUtc
    {
        get
        {
            lock (gate)
            {
                return pending.Values
                    .OrderBy(static publication => publication.Schedule.ScheduledForUtc)
                    .FirstOrDefault()
                    ?.Schedule
                    .ScheduledForUtc;
            }
        }
    }

    public async ValueTask<Dictionary<string, string>> ScheduleAsync(
        EventPublication publication,
        EventPublicationSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);
        cancellationToken.ThrowIfCancellationRequested();

        if (!options.EnablePublicationScheduling)
        {
            throw new ArgumentException(
                "Publication scheduling is not enabled for the active eventing runtime.",
                nameof(publication));
        }

        if (!channels.TryGet(publication.ChannelId, out _))
        {
            throw new InvalidOperationException(
                $"Event channel '{publication.ChannelId}' is not registered in the active eventing runtime.");
        }

        var delayMilliseconds = Math.Max(0, schedule.DelayMilliseconds);
        if (delayMilliseconds > options.PublicationSchedulingMaxDelayMilliseconds)
        {
            throw new ArgumentException(
                $"Publication delay {delayMilliseconds.ToString(CultureInfo.InvariantCulture)}ms exceeds the configured maximum of {options.PublicationSchedulingMaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture)}ms.",
                nameof(publication));
        }

        Dictionary<string, string> metadata;
        lock (gate)
        {
            ThrowIfDisposed();
            if (pending.Count >= options.PublicationSchedulingMaxPendingCount)
            {
                throw new InvalidOperationException(
                    $"Publication scheduling pending count has reached the configured maximum of {options.PublicationSchedulingMaxPendingCount.ToString(CultureInfo.InvariantCulture)}.");
            }

            if (pending.ContainsKey(publication.Id))
            {
                throw new InvalidOperationException(
                    $"Publication '{publication.Id}' is already pending in the process-local scheduler.");
            }

            metadata = EventPublicationSchedulingPolicy.CreateMetadata(
                publication.Metadata,
                schedule,
                "scheduled",
                pending.Count + 1);
            metadata["scheduleDispatch"] = "pending";
            var scheduledPublication = new ScheduledPublication(
                CopyPublication(publication, metadata),
                schedule);
            pending.Add(publication.Id, scheduledPublication);
            RescheduleTimerUnsafe();
        }

        await publicationRuntimeReporter.ReportAsync(
            new EventPublicationRuntimeReport(
                publicationId: publication.Id,
                channelId: publication.ChannelId,
                eventType: publication.EventType,
                outcome: EventPublicationRuntimeOutcomes.Accepted,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: metadata),
            cancellationToken).ConfigureAwait(false);

        return metadata;
    }

    public async ValueTask DisposeAsync()
    {
        Timer? timerToDispose;
        lock (gate)
        {
            disposed = true;
            pending.Clear();
            timerToDispose = timer;
            timer = null;
        }

        if (timerToDispose is not null)
        {
            await timerToDispose.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void OnTimer()
    {
        _ = DispatchDueAsync();
    }

    private async Task DispatchDueAsync()
    {
        if (Interlocked.Exchange(ref dispatching, 1) == 1)
        {
            return;
        }

        try
        {
            while (true)
            {
                ScheduledPublication[] due;
                lock (gate)
                {
                    if (disposed)
                    {
                        return;
                    }

                    var nowUtc = DateTimeOffset.UtcNow;
                    due = pending.Values
                        .Where(publication => publication.Schedule.ScheduledForUtc <= nowUtc)
                        .OrderBy(static publication => publication.Schedule.ScheduledForUtc)
                        .ToArray();
                    foreach (var publication in due)
                    {
                        pending.Remove(publication.Publication.Id);
                    }

                    RescheduleTimerUnsafe();
                }

                if (due.Length == 0)
                {
                    return;
                }

                foreach (var scheduledPublication in due)
                {
                    await DispatchOneAsync(scheduledPublication).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref dispatching, 0);
            lock (gate)
            {
                if (!disposed)
                {
                    RescheduleTimerUnsafe();
                }
            }
        }
    }

    private async Task DispatchOneAsync(ScheduledPublication scheduledPublication)
    {
        var publication = scheduledPublication.Publication;
        var metadata = EventPublicationSchedulingPolicy.CreateMetadata(
            publication.Metadata,
            scheduledPublication.Schedule,
            "due",
            PendingCount);
        metadata["scheduleDispatch"] = "started";
        metadata["scheduleDispatchedAtUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var duePublication = CopyPublication(publication, metadata);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(duePublication).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            metadata["scheduleState"] = "dispatch-failed";
            metadata["scheduleDispatch"] = "failed";
            await publicationRuntimeReporter.ReportAsync(
                new EventPublicationRuntimeReport(
                    publicationId: publication.Id,
                    channelId: publication.ChannelId,
                    eventType: publication.EventType,
                    outcome: EventPublicationRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    error: exception.Message,
                    metadata: metadata)).ConfigureAwait(false);
        }
    }

    private void RescheduleTimerUnsafe()
    {
        if (disposed)
        {
            return;
        }

        timer ??= new Timer(static state => ((EventPublicationScheduleQueue)state!).OnTimer(), this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        var next = pending.Values
            .OrderBy(static publication => publication.Schedule.ScheduledForUtc)
            .FirstOrDefault();
        if (next is null)
        {
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }

        var delay = next.Schedule.ScheduledForUtc - DateTimeOffset.UtcNow;
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        timer.Change(delay, Timeout.InfiniteTimeSpan);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static EventPublication CopyPublication(
        EventPublication publication,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new EventPublication(
            id: publication.Id,
            channelId: publication.ChannelId,
            eventType: publication.EventType,
            payload: publication.Payload,
            occurredAtUtc: publication.OccurredAtUtc,
            contentType: publication.ContentType,
            correlationId: publication.CorrelationId,
            tenantId: publication.TenantId,
            headers: publication.Headers,
            metadata: metadata);
    }

    private sealed record ScheduledPublication(
        EventPublication Publication,
        EventPublicationSchedule Schedule);
}
