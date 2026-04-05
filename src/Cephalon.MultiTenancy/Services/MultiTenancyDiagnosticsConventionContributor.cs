using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Services;

internal sealed class MultiTenancyDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MultiTenancyDiagnosticsConventions.Convention;
}

internal static class MultiTenancyDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition TenantResolved = new(
        Id: 4500,
        Name: "TenantResolved",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Resolved tenant '{TenantId}' from source '{Source}'.",
        Description: "Emitted when the default Cephalon multi-tenancy resolver resolves a configured tenant.");

    public static readonly DiagnosticEventDefinition TenantResolutionDefaulted = new(
        Id: 4501,
        Name: "TenantResolutionDefaulted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Resolved tenant '{TenantId}' through fallback source '{Source}'.",
        Description: "Emitted when tenant resolution succeeds through a configured default tenant or single-tenant fallback.");

    public static readonly DiagnosticEventDefinition TenantResolutionMissed = new(
        Id: 4502,
        Name: "TenantResolutionMissed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "No configured tenant matched the current request. Reason: {Reason}.",
        Description: "Emitted when the default Cephalon multi-tenancy resolver cannot resolve a tenant from the supplied request hints.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy",
        Description: "Structured diagnostics for the default Cephalon multi-tenancy resolver.",
        Events:
        [
            TenantResolved,
            TenantResolutionDefaulted,
            TenantResolutionMissed
        ]);
}
