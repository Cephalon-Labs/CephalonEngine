using Cephalon.Abstractions.AppModel.Scaffolding;

namespace Cephalon.Engine.AppModel.Scaffolding;

public static class BuiltInScaffolds
{
    public static ScaffoldPlan ModularMonolith { get; } = new(
        id: "modular-monolith",
        displayName: "Modular Monolith Scaffold",
        description: "A single ASP.NET Core host with module libraries organized around application, domain, and infrastructure boundaries.",
        projects:
        [
            new ScaffoldProject(
                id: "host",
                nameTemplate: "{AppName}.Host",
                pathTemplate: "src/{AppName}.Host",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Host,
                template: "cephalon-web-host",
                dependsOn: ["foundation", "module"],
                packages: ["Cephalon.AspNetCore", "Cephalon.Observability"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hostKind"] = "aspnet-core"
                }),
            new ScaffoldProject(
                id: "foundation",
                nameTemplate: "{AppName}.Foundation",
                pathTemplate: "src/{AppName}.Foundation",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Foundation,
                template: "cephalon-foundation",
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "module",
                nameTemplate: "{AppName}.Modules.{ModuleName}",
                pathTemplate: "src/{AppName}.Modules.{ModuleName}",
                scope: ScaffoldScopes.Module,
                role: ProjectRoles.Module,
                template: "cephalon-module",
                dependsOn: ["foundation"],
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "tests",
                nameTemplate: "{AppName}.Tests",
                pathTemplate: "tests/{AppName}.Tests",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Tests,
                template: "cephalon-tests",
                dependsOn: ["host", "module"])
        ],
        folders:
        [
            new ScaffoldFolder("Configuration", "Engine, transport, and module registration configuration for the host.", ScaffoldScopes.Solution, projectId: "host"),
            new ScaffoldFolder("Application", "Use cases, orchestration, and module-facing services.", ScaffoldScopes.Module, projectId: "module"),
            new ScaffoldFolder("Domain", "Entities, value objects, domain services, and core business rules.", ScaffoldScopes.Module, projectId: "module"),
            new ScaffoldFolder("Infrastructure", "Persistence, gateway implementations, and external integrations.", ScaffoldScopes.Module, projectId: "module"),
            new ScaffoldFolder("Endpoints", "Transport-facing adapters exposed by the module.", ScaffoldScopes.Module, projectId: "module"),
            new ScaffoldFolder("Strategies", "Optional strategy implementations for swappable behaviors.", ScaffoldScopes.Module, projectId: "module")
        ],
        conventions:
        [
            "Keep the host thin; compose modules, register transports, and expose health or manifest routes there.",
            "Keep business logic inside modules and use configuration-driven discovery where possible.",
            "Use shared foundation packages for contracts, diagnostics, and runtime policies."
        ],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organizationStyle"] = "module-first"
        });

    public static ScaffoldPlan ModularVerticalSlice { get; } = new(
        id: "modular-vertical-slice",
        displayName: "Modular Vertical Slice Scaffold",
        description: "A single ASP.NET Core host with modules organized by feature slices inside each bounded module.",
        projects:
        [
            new ScaffoldProject(
                id: "host",
                nameTemplate: "{AppName}.Host",
                pathTemplate: "src/{AppName}.Host",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Host,
                template: "cephalon-web-host",
                dependsOn: ["foundation", "module"],
                packages: ["Cephalon.AspNetCore", "Cephalon.Observability"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hostKind"] = "aspnet-core"
                }),
            new ScaffoldProject(
                id: "foundation",
                nameTemplate: "{AppName}.Foundation",
                pathTemplate: "src/{AppName}.Foundation",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Foundation,
                template: "cephalon-foundation",
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "module",
                nameTemplate: "{AppName}.Modules.{ModuleName}",
                pathTemplate: "src/{AppName}.Modules.{ModuleName}",
                scope: ScaffoldScopes.Module,
                role: ProjectRoles.Module,
                template: "cephalon-module",
                dependsOn: ["foundation"],
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "tests",
                nameTemplate: "{AppName}.Tests",
                pathTemplate: "tests/{AppName}.Tests",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Tests,
                template: "cephalon-tests",
                dependsOn: ["host", "module"])
        ],
        folders:
        [
            new ScaffoldFolder("Configuration", "Engine, transport, and module registration configuration for the host.", ScaffoldScopes.Solution, projectId: "host"),
            new ScaffoldFolder("Features/{FeatureName}/Commands", "Write-side request handlers, validators, and workflows.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Queries", "Read-side query handlers, projections, and query contracts.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Endpoints", "REST, JSON-RPC, gRPC, SSE, or WebSocket transport adapters for the slice.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Contracts", "Feature DTOs, request contracts, and event payloads.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Policies", "Cross-cutting rules, authorization policies, and pipeline behaviors for the slice.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Strategies", "Optional strategy implementations scoped to the feature slice.", ScaffoldScopes.Feature, projectId: "module")
        ],
        conventions:
        [
            "Group handlers, endpoints, contracts, and policies by feature so ownership stays close to the use case.",
            "Keep cross-slice collaboration explicit through contracts, events, or module capabilities.",
            "Use the host only for composition and transport registration; keep feature logic in the module."
        ],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organizationStyle"] = "vertical-slice"
        });

    public static ScaffoldPlan Microservice { get; } = new(
        id: "microservice",
        displayName: "Microservice Scaffold",
        description: "An independently deployable service host with explicit service contracts and modular feature slices inside the boundary.",
        projects:
        [
            new ScaffoldProject(
                id: "host",
                nameTemplate: "{AppName}.Service",
                pathTemplate: "src/{AppName}.Service",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Host,
                template: "cephalon-service-host",
                dependsOn: ["foundation", "contracts", "module"],
                packages: ["Cephalon.AspNetCore", "Cephalon.Observability"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hostKind"] = "aspnet-core"
                }),
            new ScaffoldProject(
                id: "foundation",
                nameTemplate: "{AppName}.Foundation",
                pathTemplate: "src/{AppName}.Foundation",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Foundation,
                template: "cephalon-foundation",
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "contracts",
                nameTemplate: "{AppName}.Contracts",
                pathTemplate: "src/{AppName}.Contracts",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Contracts,
                template: "cephalon-contracts",
                dependsOn: ["foundation"],
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "module",
                nameTemplate: "{AppName}.Modules.{ModuleName}",
                pathTemplate: "src/{AppName}.Modules.{ModuleName}",
                scope: ScaffoldScopes.Module,
                role: ProjectRoles.Module,
                template: "cephalon-module",
                dependsOn: ["foundation", "contracts"],
                packages: ["Cephalon.Abstractions"]),
            new ScaffoldProject(
                id: "tests",
                nameTemplate: "{AppName}.Service.Tests",
                pathTemplate: "tests/{AppName}.Service.Tests",
                scope: ScaffoldScopes.Solution,
                role: ProjectRoles.Tests,
                template: "cephalon-tests",
                dependsOn: ["host", "module"])
        ],
        folders:
        [
            new ScaffoldFolder("Configuration", "Environment-specific service configuration, policies, and transport registration.", ScaffoldScopes.Solution, projectId: "host"),
            new ScaffoldFolder("Features/{FeatureName}/Api", "Transport-facing endpoints or RPC services for the feature boundary.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Application", "Feature workflows, orchestration, and service-level business logic.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Contracts", "Service contracts, integration events, and boundary DTOs.", ScaffoldScopes.Feature, projectId: "module"),
            new ScaffoldFolder("Features/{FeatureName}/Policies", "Resilience, authorization, and policy-driven behaviors for the service.", ScaffoldScopes.Feature, projectId: "module")
        ],
        conventions:
        [
            "Treat the service boundary as explicit; keep public contracts versionable and isolated from internal module details.",
            "Use modules and slices inside the service so growth does not collapse into one large service layer.",
            "Keep host configuration environment-aware and let transports remain adapters over the same application model."
        ],
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["deploymentTopology"] = "microservice"
        });
}
