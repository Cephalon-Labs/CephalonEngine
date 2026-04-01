namespace Cephalon.Playground.Services;

public interface IEngineClock
{
    DateTimeOffset GetUtcNow();
}
