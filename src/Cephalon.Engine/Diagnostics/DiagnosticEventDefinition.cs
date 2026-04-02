namespace Cephalon.Engine.Diagnostics;

/// <summary>
/// Describes one published diagnostics event id together with its intended meaning.
/// </summary>
/// <param name="Id">The stable numeric event identifier.</param>
/// <param name="Name">The stable event name paired with the numeric identifier.</param>
/// <param name="Severity">The intended severity for the event.</param>
/// <param name="MessageTemplate">The structured message template emitted by the logger.</param>
/// <param name="Description">The operator-facing explanation of when the event is emitted.</param>
public sealed record DiagnosticEventDefinition(
    int Id,
    string Name,
    DiagnosticSeverity Severity,
    string MessageTemplate,
    string Description);
