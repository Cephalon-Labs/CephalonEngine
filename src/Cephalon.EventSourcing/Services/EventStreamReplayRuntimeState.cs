namespace Cephalon.EventSourcing.Services;

internal sealed class EventStreamReplayRuntimeState
{
    private readonly object sync = new();
    private EventStreamReplayReport? latestReport;

    public EventStreamReplayReport? LatestReport
    {
        get
        {
            lock (sync)
            {
                return latestReport;
            }
        }
    }

    public void Record(EventStreamReplayReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        lock (sync)
        {
            latestReport = report;
        }
    }
}
