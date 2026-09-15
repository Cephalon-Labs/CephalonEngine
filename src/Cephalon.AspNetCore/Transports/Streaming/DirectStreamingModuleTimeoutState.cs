using System.Globalization;

namespace Cephalon.AspNetCore.Transports.Streaming;

internal sealed class DirectStreamingModuleTimeoutState
{
    private readonly DirectStreamingModuleResilienceOptions options;
    private long timeoutCount;
    private long lastTimeoutAtUtcTicks;

    public DirectStreamingModuleTimeoutState(DirectStreamingModuleResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    public void RecordTimeout()
    {
        Interlocked.Increment(ref timeoutCount);
        Interlocked.Exchange(ref lastTimeoutAtUtcTicks, DateTimeOffset.UtcNow.UtcTicks);
    }

    public IReadOnlyDictionary<string, string> CreateMetadata()
    {
        if (!options.TimeoutEnabled)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["timeoutOccurredCount"] = Interlocked.Read(ref timeoutCount).ToString(CultureInfo.InvariantCulture)
        };

        var ticks = Interlocked.Read(ref lastTimeoutAtUtcTicks);
        if (ticks > 0)
        {
            metadata["timeoutLastOccurredAtUtc"] = new DateTimeOffset(ticks, TimeSpan.Zero)
                .ToString("O", CultureInfo.InvariantCulture);
        }

        return metadata;
    }
}
