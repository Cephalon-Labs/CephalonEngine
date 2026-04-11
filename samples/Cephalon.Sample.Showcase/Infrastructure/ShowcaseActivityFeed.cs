using System.Threading.Channels;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Keeps a lightweight in-memory activity feed so the showcase UI can render live server-backed activity.
/// </summary>
internal sealed class ShowcaseActivityFeed
{
    private const int MaxEntries = 250;
    private readonly Lock syncRoot = new();
    private readonly List<ShowcaseActivityEntry> entries = [];
    private readonly Dictionary<Guid, Channel<ShowcaseActivityEntry>> subscribers = new();
    private long totalRecorded;

    public long TotalRecorded
    {
        get
        {
            lock (syncRoot)
            {
                return totalRecorded;
            }
        }
    }

    public ShowcaseActivityEntry Record(
        string area,
        string transport,
        string method,
        string path,
        int statusCode,
        double durationMs,
        string? title = null)
    {
        var outcome = statusCode >= 500
            ? "error"
            : statusCode >= 400
                ? "warning"
                : "success";

        ShowcaseActivityEntry entry;
        Channel<ShowcaseActivityEntry>[] subscribersSnapshot;

        lock (syncRoot)
        {
            totalRecorded++;
            entry = new ShowcaseActivityEntry(
                totalRecorded,
                DateTimeOffset.UtcNow,
                area,
                transport,
                method,
                path,
                statusCode,
                Math.Round(durationMs, 2),
                outcome,
                title);

            entries.Insert(0, entry);
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            }

            subscribersSnapshot = subscribers.Values.ToArray();
        }

        foreach (var subscriber in subscribersSnapshot)
        {
            subscriber.Writer.TryWrite(entry);
        }

        return entry;
    }

    public IReadOnlyList<ShowcaseActivityEntry> GetRecent(int limit = 50)
    {
        var normalizedLimit = Math.Clamp(limit, 1, MaxEntries);

        lock (syncRoot)
        {
            return entries
                .Take(normalizedLimit)
                .ToArray();
        }
    }

    public void Clear()
    {
        lock (syncRoot)
        {
            entries.Clear();
            totalRecorded = 0;
        }
    }

    public ChannelReader<ShowcaseActivityEntry> Subscribe(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<ShowcaseActivityEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        var subscriptionId = Guid.NewGuid();

        lock (syncRoot)
        {
            subscribers[subscriptionId] = channel;
        }

        cancellationToken.Register(() => Unsubscribe(subscriptionId, channel.Writer));
        return channel.Reader;
    }

    private void Unsubscribe(Guid subscriptionId, ChannelWriter<ShowcaseActivityEntry> writer)
    {
        lock (syncRoot)
        {
            subscribers.Remove(subscriptionId);
        }

        writer.TryComplete();
    }
}
