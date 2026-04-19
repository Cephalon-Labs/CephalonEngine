namespace Cephalon.Behaviors.Patterns.Runtime;

internal static class DurableExecutionRuntimeOutcomes
{
    public const string Started = "started";
    public const string Succeeded = "succeeded";
    public const string ContinuationStaged = "continuation-staged";
    public const string Waiting = "waiting";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
