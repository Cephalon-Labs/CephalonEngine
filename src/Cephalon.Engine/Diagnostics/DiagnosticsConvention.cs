namespace Cephalon.Engine.Diagnostics;

/// <summary>
/// Describes the diagnostics convention published by one engine package or companion package.
/// </summary>
/// <param name="Source">The stable package or subsystem identifier that owns the convention.</param>
/// <param name="LoggerCategoryPrefix">The logger category prefix used by the convention.</param>
/// <param name="Description">The operator-facing explanation of what the convention covers.</param>
/// <param name="Events">The published event definitions that belong to the convention.</param>
public sealed record DiagnosticsConvention(
    string Source,
    string LoggerCategoryPrefix,
    string Description,
    IReadOnlyList<DiagnosticEventDefinition> Events)
{
    /// <summary>
    /// Gets the smallest event id published by this convention.
    /// </summary>
    public int? MinimumEventId => Events.Count == 0 ? null : Events.Min(static item => item.Id);

    /// <summary>
    /// Gets the largest event id published by this convention.
    /// </summary>
    public int? MaximumEventId => Events.Count == 0 ? null : Events.Max(static item => item.Id);
}
