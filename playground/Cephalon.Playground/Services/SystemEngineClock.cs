namespace Cephalon.Playground.Services;

public sealed class SystemEngineClock : IEngineClock
{
    public DateTimeOffset GetUtcNow()
    {
        return TimeProvider.System.GetUtcNow();
    }
}
