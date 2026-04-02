namespace Cephalon.Engine.Diagnostics;

internal sealed class EngineDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => EngineRuntimeDiagnosticsConventions.Convention;
}

internal static class EngineRuntimeDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition RuntimeTransition = new(
        Id: 2000,
        Name: "LogRuntimeTransition",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Runtime phase '{Phase}' completed with status {Status}. Blueprint {BlueprintId}. Modules {ModuleCount}.",
        Description: "Emitted after a runtime lifecycle phase completes successfully.");

    public static readonly DiagnosticEventDefinition ModuleTransition = new(
        Id: 2001,
        Name: "LogModuleTransition",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Module '{ModuleId}' completed phase '{Phase}' with version {Version}.",
        Description: "Emitted after one module completes a lifecycle phase successfully.");

    public static readonly DiagnosticEventDefinition RuntimeFailure = new(
        Id: 2002,
        Name: "LogRuntimeFailure",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Runtime phase '{Phase}' failed while status was {Status}.",
        Description: "Emitted when a runtime lifecycle phase fails.");

    public static readonly DiagnosticEventDefinition ModuleFailure = new(
        Id: 2003,
        Name: "LogModuleFailure",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Module '{ModuleId}' failed during phase '{Phase}'.",
        Description: "Emitted when one module fails during a lifecycle phase.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Engine",
        LoggerCategoryPrefix: "Cephalon.Engine",
        Description: "Structured lifecycle diagnostics for runtime and module transitions, failures, and restart-related operator analysis.",
        Events:
        [
            RuntimeTransition,
            ModuleTransition,
            RuntimeFailure,
            ModuleFailure
        ]);
}
