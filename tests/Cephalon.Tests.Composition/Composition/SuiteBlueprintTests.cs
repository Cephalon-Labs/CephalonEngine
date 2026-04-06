using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.AppModel.Scaffolding;

namespace Cephalon.Tests.Composition;

public sealed class SuiteBlueprintTests
{
    [Fact]
    public void BuiltInSuiteBlueprintsExposeMicroserviceSuiteComposedFromMicroserviceBlueprint()
    {
        Assert.True(BuiltInSuiteBlueprints.TryResolve("MicroserviceSuite", out var blueprint));
        Assert.Equal("microservice-suite", blueprint.Id);
        Assert.Equal("Microservice Suite", blueprint.DisplayName);
        Assert.Equal(BuiltInBlueprints.Microservice.Id, blueprint.Metadata["serviceBlueprintId"]);

        var suiteScaffold = blueprint.Scaffold;
        var service = Assert.Single(suiteScaffold.Services);
        var sharedProject = Assert.Single(suiteScaffold.SharedProjects);
        var sharedFolder = Assert.Single(suiteScaffold.SharedFolders);

        Assert.Equal(BuiltInBlueprints.Microservice.Id, service.BlueprintId);
        Assert.Equal("repeatable", service.Metadata["cardinality"]);
        Assert.Contains("shared-foundation", service.DependsOn);
        Assert.Equal(ScaffoldScopes.Suite, sharedProject.Scope);
        Assert.Equal(ProjectRoles.Foundation, sharedProject.Role);
        Assert.Equal("shared-foundation", sharedFolder.ProjectId);
        Assert.Equal(BuiltInScaffolds.Microservice.Id, suiteScaffold.Metadata["derivedFromScaffoldId"]);
    }

    [Fact]
    public void BuiltInSuiteBlueprintsReuseMicroserviceProjectTemplatesInsteadOfRedefiningThem()
    {
        var serviceScaffold = Assert.IsType<ScaffoldPlan>(BuiltInBlueprints.Microservice.Scaffold);
        var suiteScaffold = BuiltInSuiteBlueprints.MicroserviceSuite.Scaffold;
        var foundationProject = Assert.Single(
            serviceScaffold.Projects,
            project => string.Equals(project.Role, ProjectRoles.Foundation, StringComparison.OrdinalIgnoreCase));
        var hostProject = Assert.Single(
            serviceScaffold.Projects,
            project => string.Equals(project.Role, ProjectRoles.Host, StringComparison.OrdinalIgnoreCase));
        var contractsProject = Assert.Single(
            serviceScaffold.Projects,
            project => string.Equals(project.Role, ProjectRoles.Contracts, StringComparison.OrdinalIgnoreCase));
        var moduleProject = Assert.Single(
            serviceScaffold.Projects,
            project => string.Equals(project.Role, ProjectRoles.Module, StringComparison.OrdinalIgnoreCase));
        var sharedProject = Assert.Single(suiteScaffold.SharedProjects);
        var service = Assert.Single(suiteScaffold.Services);

        Assert.Equal(foundationProject.Template, sharedProject.Template);
        Assert.Equal(foundationProject.Packages, sharedProject.Packages);
        Assert.Equal(hostProject.Id, service.Metadata["hostProjectId"]);
        Assert.Equal(contractsProject.Id, service.Metadata["contractsProjectId"]);
        Assert.Equal(moduleProject.Id, service.Metadata["moduleProjectId"]);
        Assert.Contains(
            suiteScaffold.Conventions,
            convention => convention.Contains(BuiltInBlueprints.Microservice.DisplayName, StringComparison.Ordinal));
    }
}
