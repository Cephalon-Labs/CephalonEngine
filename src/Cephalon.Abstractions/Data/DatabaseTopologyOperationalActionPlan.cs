namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the engine-owned ordered operator action plan for the current database-topology posture.
/// </summary>
public sealed class DatabaseTopologyOperationalActionPlan
{
    /// <summary>
    /// Creates a new database-topology action plan.
    /// </summary>
    /// <param name="generatedAtUtc">The UTC timestamp when the plan was created.</param>
    /// <param name="actions">The ordered operator actions derived from the current topology posture.</param>
    public DatabaseTopologyOperationalActionPlan(
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<DatabaseTopologyOperationalAction>? actions = null)
    {
        GeneratedAtUtc = generatedAtUtc;
        Actions = actions?.ToArray() ?? [];
        TotalActionCount = Actions.Count;
        BlockingActionCount = Actions.Count(static action =>
            string.Equals(action.Tone, "Error", StringComparison.OrdinalIgnoreCase));
        AttentionActionCount = Actions.Count(static action =>
            string.Equals(action.Tone, "Warning", StringComparison.OrdinalIgnoreCase));
        ReadyActionCount = Actions.Count(static action =>
            string.Equals(action.Tone, "Success", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the UTC timestamp when the plan was created.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>
    /// Gets the total number of operator actions in the plan.
    /// </summary>
    public int TotalActionCount { get; }

    /// <summary>
    /// Gets the number of blocking actions in the plan.
    /// </summary>
    public int BlockingActionCount { get; }

    /// <summary>
    /// Gets the number of attention-level actions in the plan.
    /// </summary>
    public int AttentionActionCount { get; }

    /// <summary>
    /// Gets the number of ready-state actions in the plan.
    /// </summary>
    public int ReadyActionCount { get; }

    /// <summary>
    /// Gets the ordered operator actions derived from the current topology posture.
    /// </summary>
    public IReadOnlyList<DatabaseTopologyOperationalAction> Actions { get; }
}
