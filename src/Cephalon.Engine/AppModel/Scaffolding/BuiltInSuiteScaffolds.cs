using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;

namespace Cephalon.Engine.AppModel.Scaffolding;

/// <summary>
/// Provides the built-in suite-scaffold plans that back shipped solution-level Cephalon blueprints.
/// </summary>
public static class BuiltInSuiteScaffolds
{
    /// <summary>
    /// Gets the suite scaffold for the microservice-suite blueprint.
    /// </summary>
    public static SuiteScaffoldPlan MicroserviceSuite { get; } = ComposeMicroserviceSuite(BuiltInBlueprints.Microservice);

    private static SuiteScaffoldPlan ComposeMicroserviceSuite(AppBlueprint serviceBlueprint)
    {
        var serviceScaffold = serviceBlueprint.Scaffold
            ?? throw new InvalidOperationException(
                $"Built-in blueprint '{serviceBlueprint.Id}' must define a scaffold plan before a suite blueprint can compose it.");
        var hostProject = GetProject(serviceScaffold, ProjectRoles.Host);
        var foundationProject = GetProject(serviceScaffold, ProjectRoles.Foundation);
        var contractsProject = GetProject(serviceScaffold, ProjectRoles.Contracts);
        var moduleProject = GetProject(serviceScaffold, ProjectRoles.Module);

        return new SuiteScaffoldPlan(
            id: "microservice-suite",
            displayName: "Microservice Suite Scaffold",
            description: "A coordinated suite of Cephalon microservices that reuses the shipped microservice scaffold for each service slot.",
            services:
            [
                new SuiteScaffoldService(
                    id: "service",
                    displayName: "Service Slot",
                    description: $"A repeatable service slot composed from the existing {serviceBlueprint.DisplayName} blueprint.",
                    blueprintId: serviceBlueprint.Id,
                    nameTemplate: "{SuiteName}.{ServiceName}",
                    pathTemplate: "services/{ServiceName}",
                    dependsOn: ["shared-foundation"],
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["cardinality"] = "repeatable",
                        ["derivedFromBlueprintId"] = serviceBlueprint.Id,
                        ["derivedFromScaffoldId"] = serviceScaffold.Id,
                        ["hostProjectId"] = hostProject.Id,
                        ["contractsProjectId"] = contractsProject.Id,
                        ["moduleProjectId"] = moduleProject.Id
                    })
            ],
            sharedProjects:
            [
                new ScaffoldProject(
                    id: "shared-foundation",
                    nameTemplate: "{SuiteName}.Shared.Foundation",
                    pathTemplate: "shared/{SuiteName}.Shared.Foundation",
                    scope: ScaffoldScopes.Suite,
                    role: foundationProject.Role,
                    template: foundationProject.Template,
                    packages: foundationProject.Packages,
                    metadata: CreateMetadata(
                        foundationProject.Metadata,
                        ("derivedFromBlueprintId", serviceBlueprint.Id),
                        ("derivedFromProjectId", foundationProject.Id)))
            ],
            sharedFolders:
            [
                new ScaffoldFolder(
                    pathTemplate: "Conventions",
                    purpose: "Suite-level conventions, governance helpers, and shared solution guidance.",
                    scope: ScaffoldScopes.Suite,
                    projectId: "shared-foundation",
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["derivedFromBlueprintId"] = serviceBlueprint.Id
                    })
            ],
            conventions:
            [
                $"Compose each service from the shipped {serviceBlueprint.DisplayName} blueprint instead of redefining host, contracts, and module project shapes at the suite layer.",
                "Keep shared suite projects limited to cross-service contracts, governance helpers, and conventions; service internals still belong to the app-level blueprint.",
                $"{serviceBlueprint.DisplayName}: {serviceScaffold.Conventions[0]}"
            ],
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["serviceBlueprintId"] = serviceBlueprint.Id,
                ["derivedFromScaffoldId"] = serviceScaffold.Id,
                ["serviceSlotStrategy"] = "repeatable"
            });
    }

    private static ScaffoldProject GetProject(ScaffoldPlan scaffold, string role)
    {
        return scaffold.Projects.FirstOrDefault(project =>
                   string.Equals(project.Role, role, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"Scaffold plan '{scaffold.Id}' must define a project with role '{role}' before suite composition can use it.");
    }

    private static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> source,
        params (string Key, string Value)[] additions)
    {
        var metadata = new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in additions)
        {
            metadata[key] = value;
        }

        return metadata;
    }
}
