using Cephalon.Abstractions.AppModel.Scaffolding;

namespace Cephalon.Tests.Scaffolding;

public sealed class SuiteScaffoldPlanTests
{
    [Fact]
    public void CreateAcceptsSuiteServiceSlotsAndSharedProjects()
    {
        var sharedProject = new ScaffoldProject(
            id: "shared-foundation",
            nameTemplate: "{SuiteName}.Shared.Foundation",
            pathTemplate: "shared/{SuiteName}.Shared.Foundation",
            scope: ScaffoldScopes.Suite,
            role: ProjectRoles.Foundation,
            template: "cephalon-foundation");
        var sharedFolder = new ScaffoldFolder(
            pathTemplate: "Conventions",
            purpose: "Shared suite conventions and governance helpers.",
            scope: ScaffoldScopes.Suite,
            projectId: "shared-foundation");
        var service = new SuiteScaffoldService(
            id: "service",
            displayName: "Service",
            description: "A service slot composed from the existing microservice blueprint.",
            blueprintId: "microservice",
            nameTemplate: "{SuiteName}.{ServiceName}",
            pathTemplate: "services/{ServiceName}",
            dependsOn: ["shared-foundation"]);

        var plan = new SuiteScaffoldPlan(
            id: "microservice-suite",
            displayName: "Microservice Suite Scaffold",
            description: "Coordinates multiple Cephalon services around shared packages and conventions.",
            services: [service],
            sharedProjects: [sharedProject],
            sharedFolders: [sharedFolder],
            conventions:
            [
                "Compose each service from the shipped microservice blueprint instead of inventing a separate service contract."
            ],
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["compositionModel"] = "existing-blueprints"
            });

        Assert.Single(plan.Services);
        Assert.Single(plan.SharedProjects);
        Assert.Single(plan.SharedFolders);
        Assert.Equal("microservice", plan.Services[0].BlueprintId);
        Assert.Equal("shared-foundation", plan.SharedFolders[0].ProjectId);
        Assert.Equal("existing-blueprints", plan.Metadata["compositionModel"]);
    }

    [Fact]
    public void CreateRejectsUnknownServiceDependencies()
    {
        var service = new SuiteScaffoldService(
            id: "service",
            displayName: "Service",
            description: "A service slot composed from the existing microservice blueprint.",
            blueprintId: "microservice",
            nameTemplate: "{SuiteName}.{ServiceName}",
            pathTemplate: "services/{ServiceName}",
            dependsOn: ["missing-foundation"]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new SuiteScaffoldPlan(
                id: "microservice-suite",
                displayName: "Microservice Suite Scaffold",
                description: "Coordinates multiple Cephalon services around shared packages and conventions.",
                services: [service]));

        Assert.Contains("depends on 'missing-foundation'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRejectsIdentityReuseAcrossServicesAndSharedProjects()
    {
        var sharedProject = new ScaffoldProject(
            id: "service",
            nameTemplate: "{SuiteName}.Shared.Foundation",
            pathTemplate: "shared/{SuiteName}.Shared.Foundation",
            scope: ScaffoldScopes.Suite,
            role: ProjectRoles.Foundation,
            template: "cephalon-foundation");
        var service = new SuiteScaffoldService(
            id: "service",
            displayName: "Service",
            description: "A service slot composed from the existing microservice blueprint.",
            blueprintId: "microservice",
            nameTemplate: "{SuiteName}.{ServiceName}",
            pathTemplate: "services/{ServiceName}");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new SuiteScaffoldPlan(
                id: "microservice-suite",
                displayName: "Microservice Suite Scaffold",
                description: "Coordinates multiple Cephalon services around shared packages and conventions.",
                services: [service],
                sharedProjects: [sharedProject]));

        Assert.Contains("used by both a service and a shared project", exception.Message, StringComparison.Ordinal);
    }
}
