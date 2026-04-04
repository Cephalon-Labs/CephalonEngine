using Cephalon.Engine.Diagnostics;

namespace Cephalon.Audit.Services;

internal sealed class AuditDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AuditDiagnosticsConventions.Convention;
}

internal static class AuditDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition AuditEntryWritten = new(
        Id: 4600,
        Name: "AuditEntryWritten",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Audit entry '{AuditEntryId}' recorded for category '{Category}' and action '{Action}'.",
        Description: "Emitted when the default Cephalon audit recorder writes an audit entry successfully.");

    public static readonly DiagnosticEventDefinition AuditEntryWriteFailed = new(
        Id: 4601,
        Name: "AuditEntryWriteFailed",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Audit entry '{AuditEntryId}' failed to record for category '{Category}' and action '{Action}'.",
        Description: "Emitted when the default Cephalon audit recorder fails to write an audit entry.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Audit",
        LoggerCategoryPrefix: "Cephalon.Audit",
        Description: "Structured diagnostics for the default Cephalon audit recording pipeline.",
        Events:
        [
            AuditEntryWritten,
            AuditEntryWriteFailed
        ]);
}
