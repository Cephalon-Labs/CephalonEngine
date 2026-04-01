namespace Cephalon.WorkerPlayground.Services;

public interface IWorkerClock
{
    DateTimeOffset GetUtcNow();
}
