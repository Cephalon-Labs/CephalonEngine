namespace Cephalon.WorkerPlayground.Services;

public sealed class SystemWorkerClock : IWorkerClock
{
    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
