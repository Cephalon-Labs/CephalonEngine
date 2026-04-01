using CephalonTemplateModule.Contracts;

namespace CephalonTemplateModule.Application;

public sealed class ModuleStatusService
{
    private readonly object sync = new();
    private int initializeCount;
    private int startCount;
    private int stopCount;
    private string currentPhase = "Created";
    private DateTimeOffset? lastTransitionUtc;

    public void MarkInitialized()
    {
        lock (sync)
        {
            initializeCount++;
            currentPhase = "Initialized";
            lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }

    public void MarkStarted()
    {
        lock (sync)
        {
            startCount++;
            currentPhase = "Started";
            lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }

    public void MarkStopped()
    {
        lock (sync)
        {
            stopCount++;
            currentPhase = "Stopped";
            lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }

    public ModuleStatusSnapshot CreateSnapshot()
    {
        lock (sync)
        {
            return new ModuleStatusSnapshot(
                InitializeCount: initializeCount,
                StartCount: startCount,
                StopCount: stopCount,
                CurrentPhase: currentPhase,
                LastTransitionUtc: lastTransitionUtc);
        }
    }
}
