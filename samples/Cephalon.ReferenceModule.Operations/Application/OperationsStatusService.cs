using Cephalon.ReferenceModule.Operations.Contracts;

namespace Cephalon.ReferenceModule.Operations.Application;

/// <summary>
/// Tracks lifecycle state for the reference operations module.
/// </summary>
public sealed class OperationsStatusService
{
    private readonly object sync = new();
    private int initializeCount;
    private int startCount;
    private int stopCount;
    private string currentPhase = "Created";
    private DateTimeOffset? lastTransitionUtc;

    /// <summary>
    /// Records that the module completed initialization.
    /// </summary>
    public void MarkInitialized()
    {
        lock (sync)
        {
            initializeCount++;
            currentPhase = "Initialized";
            lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Records that the module completed startup.
    /// </summary>
    public void MarkStarted()
    {
        lock (sync)
        {
            startCount++;
            currentPhase = "Started";
            lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Records that the module completed shutdown.
    /// </summary>
    public void MarkStopped()
    {
        lock (sync)
        {
            stopCount++;
            currentPhase = "Stopped";
            lastTransitionUtc = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Creates a transport-safe snapshot of the current lifecycle status.
    /// </summary>
    /// <param name="culture">
    /// The culture used to localize the response message.
    /// </param>
    /// <param name="message">
    /// The localized status message to include in the response.
    /// </param>
    /// <returns>
    /// A snapshot of the current lifecycle counters and message payload.
    /// </returns>
    public OperationsStatusEnvelope CreateEnvelope(string culture, string message)
    {
        lock (sync)
        {
            return new OperationsStatusEnvelope(
                InitializeCount: initializeCount,
                StartCount: startCount,
                StopCount: stopCount,
                CurrentPhase: currentPhase,
                LastTransitionUtc: lastTransitionUtc,
                Culture: culture,
                Message: message);
        }
    }
}
