using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Scaffolding.Generation;
using Cephalon.Scaffolding.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Linq;

namespace Cephalon.Tests.Scaffolding;

public sealed class ScaffoldGeneratorTests
{
    [Fact]
    public void GenerateMaterializesProjectsFoldersAndFilesFromAppProfileScaffold()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            transports: ["JsonRpc", "Grpc", "GraphQL"],
            technologies: ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "EdgeNativeDelivery"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Explorer",
                modules: ["Platform", "Discovery"],
                features: ["Greetings"],
                cephalonPackageVersion: "9.1.0-preview"));

        Assert.Contains(scaffold.Projects, project =>
            project.Name == "Acme.Explorer.Host" &&
            project.Packages.Contains("Cephalon.Agentics", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Eventing", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Edge", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Retrieval", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.GraphQL", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.JsonRpc", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.Grpc", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Engine.SourceGen", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Observability.Serilog", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Serilog.Sinks.Console", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(scaffold.Folders, folder =>
            folder.Path == "src/Acme.Explorer.Modules.Platform/Features/Greetings/Commands");

        var generatedHostProject = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Explorer.Host/Acme.Explorer.Host.csproj");
        Assert.Contains("<PackageReference Include=\"Cephalon.Engine.SourceGen\" PrivateAssets=\"all\" />", generatedHostProject.Contents, StringComparison.Ordinal);

        var solution = Assert.Single(scaffold.Files, file => file.Path == "Acme.Explorer.slnx");
        Assert.Contains("src/Acme.Explorer.Host/Acme.Explorer.Host.csproj", solution.Contents, StringComparison.Ordinal);

        var appSettings = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Explorer.Host/appsettings.json");
        Assert.Equal("{}", appSettings.Contents.Trim(), ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);

        var appSettingsDevelopment = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Explorer.Host/appsettings.Development.json");
        Assert.Equal("{}", appSettingsDevelopment.Contents.Trim(), ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);

        var observabilityDevelopment = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/Observability/Development.json");
        Assert.Contains("\"Serilog\"", observabilityDevelopment.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Serilog.Sinks.Console\"", observabilityDevelopment.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Console\"", observabilityDevelopment.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Application\": \"Acme.Explorer.Host\"", observabilityDevelopment.Contents, StringComparison.Ordinal);

        var appModelSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/AddEngine.AppModel.json");
        Assert.Contains("Acme.Explorer.Modules.Platform", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"json-rpc\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"grpc\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"graphql\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Technologies\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"agentic-workloads\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"event-driven-integration\"", appModelSettings.Contents, StringComparison.Ordinal);

        var messagingSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/AddEngine.Messaging.json");
        Assert.Contains("\"Messaging\"", messagingSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Provider\": \"Wolverine\"", messagingSettings.Contents, StringComparison.Ordinal);

        var localizationSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/AddEngine.Localization.json");
        Assert.Contains("\"Localization\"", localizationSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"engine.docs.rest.title\"", localizationSettings.Contents, StringComparison.Ordinal);

        var observabilitySettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/AddEngine.Observability.json");
        Assert.Contains("\"Telemetry\"", observabilitySettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Protocol\": \"otlp/http\"", observabilitySettings.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Endpoint\"", observabilitySettings.Contents, StringComparison.Ordinal);

        var openApiSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/AddOpenApi.json");
        Assert.Contains("\"OpenApi\"", openApiSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Title\": \"Acme.Explorer API\"", openApiSettings.Contents, StringComparison.Ordinal);

        var referenceDocsSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/AddReferenceDocs.json");
        Assert.Contains("\"ReferenceDocs\"", referenceDocsSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Enabled\": false", referenceDocsSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"DirectoryPath\": \"..\\\\..\\\\docs\\\\reference\"", referenceDocsSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"DefaultDocument\": \"browse.html\"", referenceDocsSettings.Contents, StringComparison.Ordinal);

        var configurationGuide = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Configurations/README.md");
        Assert.Contains("Configurations/Add*.json", configurationGuide.Contents, StringComparison.Ordinal);
        Assert.Contains("Configurations/{group}/{Environment}.json", configurationGuide.Contents, StringComparison.Ordinal);
        Assert.Contains("appsettings.json", configurationGuide.Contents, StringComparison.Ordinal);

        var packageProps = Assert.Single(scaffold.Files, file => file.Path == "Directory.Packages.props");
        Assert.Contains("Cephalon.Agentics", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Eventing", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Eventing.Wolverine", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Edge", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Retrieval", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.GraphQL", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.JsonRpc", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.SourceGen", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.Serilog", packageProps.Contents, StringComparison.Ordinal);

        var hostProject = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Acme.Explorer.Host.csproj");
        Assert.Contains("<Content Update=\"Configurations\\**\\*.json\">", hostProject.Contents, StringComparison.Ordinal);
        Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", hostProject.Contents, StringComparison.Ordinal);
        Assert.Contains("Microsoft.Extensions.Hosting.WindowsServices", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Serilog.Sinks.Console", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Version=\"9.1.0-preview\"", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Version=\"6.1.1\"", packageProps.Contents, StringComparison.Ordinal);

        var phase8HostProgram = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Explorer.Host/Program.cs");
        Assert.Contains("builder.AddCephalonProjectConfigurations();", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("builder.Configuration.AddEnvironmentVariables();", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.AddCephalon(engine =>", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddEventing();", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddWolverineEventing();", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.Configuration.GetSection(\"Serilog\").Exists()", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.Logging.ClearProviders();", phase8HostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.AddCephalonSerilog();", phase8HostProgram.Contents, StringComparison.Ordinal);

        var compositionSmokeTest = Assert.Single(
            scaffold.Files,
            file => file.Path == "tests/Acme.Explorer.Tests/Architecture/CompositionSmokeTests.cs");
        Assert.Contains("namespace Acme.Explorer.Tests.Architecture;", compositionSmokeTest.Contents, StringComparison.Ordinal);
        Assert.Contains("Generated_scaffold_has_a_test_harness_ready_for_real_composition_checks", compositionSmokeTest.Contents, StringComparison.Ordinal);

        var behaviorSpecification = Assert.Single(
            scaffold.Files,
            file => file.Path == "tests/Acme.Explorer.Tests/Features/GreetingsBehaviorSpecifications.cs");
        Assert.Contains("namespace Acme.Explorer.Tests.Features;", behaviorSpecification.Contents, StringComparison.Ordinal);
        Assert.Contains("Given_greetings_behavior_when_you_start_tdd_then_replace_this_placeholder_with_the_first_failing_specification", behaviorSpecification.Contents, StringComparison.Ordinal);

        var directoryBuildProps = Assert.Single(scaffold.Files, file => file.Path == "Directory.Build.props");
        Assert.Contains("<GenerateDocumentationFile>true</GenerateDocumentationFile>", directoryBuildProps.Contents, StringComparison.Ordinal);

        var nuGetConfig = Assert.Single(scaffold.Files, file => file.Path == "NuGet.config");
        Assert.Contains("./.cephalon/packages", nuGetConfig.Contents, StringComparison.Ordinal);
        Assert.Contains("packageSourceMapping", nuGetConfig.Contents, StringComparison.Ordinal);

        var localFeedReadme = Assert.Single(scaffold.Files, file => file.Path == ".cephalon/packages/README.md");
        Assert.Contains("publish-package-artifacts.ps1", localFeedReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", localFeedReadme.Contents, StringComparison.Ordinal);

        var publishProfile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Properties/PublishProfiles/CephalonFolder.pubxml");
        Assert.Contains("PublishDir", publishProfile.Contents, StringComparison.Ordinal);
        Assert.Contains("UseAppHost>false", publishProfile.Contents, StringComparison.Ordinal);
        Assert.Contains("../../artifacts/publish", publishProfile.Contents, StringComparison.Ordinal);

        var windowsServiceReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/windows-service/README.md");
        Assert.Contains("install-service.ps1", windowsServiceReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("C:\\Services\\Acme.Explorer\\current", windowsServiceReadme.Contents, StringComparison.Ordinal);

        var windowsInstallScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/windows-service/install-service.ps1");
        Assert.Contains("sc.exe create", windowsInstallScript.Contents, StringComparison.Ordinal);
        Assert.Contains("--contentRoot", windowsInstallScript.Contents, StringComparison.Ordinal);
        Assert.Contains("Acme.Explorer.Host.dll", windowsInstallScript.Contents, StringComparison.Ordinal);

        var windowsRemoveScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/windows-service/remove-service.ps1");
        Assert.Contains("sc.exe delete", windowsRemoveScript.Contents, StringComparison.Ordinal);

        var iisReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/iis/README.md");
        Assert.Contains("install-site.ps1", iisReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("AspNetCoreModuleV2", iisReadme.Contents, StringComparison.Ordinal);

        var iisInstallScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/iis/install-site.ps1");
        Assert.Contains("add apppool", iisInstallScript.Contents, StringComparison.Ordinal);
        Assert.Contains("add site", iisInstallScript.Contents, StringComparison.Ordinal);
        Assert.Contains("C:\\inetpub\\sites\\Acme.Explorer\\current", iisInstallScript.Contents, StringComparison.Ordinal);

        var iisRemoveScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/iis/remove-site.ps1");
        Assert.Contains("delete site", iisRemoveScript.Contents, StringComparison.Ordinal);
        Assert.Contains("delete apppool", iisRemoveScript.Contents, StringComparison.Ordinal);

        var azureAppServiceReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/azure-app-service/README.md");
        Assert.Contains("deploy-zip.ps1", azureAppServiceReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", azureAppServiceReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("az webapp deploy", azureAppServiceReadme.Contents, StringComparison.Ordinal);

        var azureAppServiceDeployScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/azure-app-service/deploy-zip.ps1");
        Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", azureAppServiceDeployScript.Contents, StringComparison.Ordinal);
        Assert.Contains("az @deployArguments", azureAppServiceDeployScript.Contents, StringComparison.Ordinal);
        Assert.Contains("azure-app-service.zip", azureAppServiceDeployScript.Contents, StringComparison.Ordinal);

        var containerImageReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/container-image/README.md");
        Assert.Contains("publish-image.ps1", containerImageReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("docker login ghcr.io", containerImageReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", containerImageReadme.Contents, StringComparison.Ordinal);

        var containerImagePublishScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/container-image/publish-image.ps1");
        Assert.Contains("Get-DockerBuildArguments", containerImagePublishScript.Contents, StringComparison.Ordinal);
        Assert.Contains("Format-Command -Command \"docker\"", containerImagePublishScript.Contents, StringComparison.Ordinal);
        Assert.Contains("@(\"push\", $tag)", containerImagePublishScript.Contents, StringComparison.Ordinal);
        Assert.Contains("Container image publishing completed successfully.", containerImagePublishScript.Contents, StringComparison.Ordinal);

        var azureContainerAppsReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/azure-container-apps/README.md");
        Assert.Contains("deploy-up.ps1", azureContainerAppsReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("az containerapp up", azureContainerAppsReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("--source", azureContainerAppsReadme.Contents, StringComparison.Ordinal);

        var azureContainerAppsDeployScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/azure-container-apps/deploy-up.ps1");
        Assert.Contains("containerapp", azureContainerAppsDeployScript.Contents, StringComparison.Ordinal);
        Assert.Contains("--source", azureContainerAppsDeployScript.Contents, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_HTTP_PORTS=8080", azureContainerAppsDeployScript.Contents, StringComparison.Ordinal);
        Assert.Contains("DOTNET_ENVIRONMENT=Production", azureContainerAppsDeployScript.Contents, StringComparison.Ordinal);

        var kubernetesReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/kubernetes/README.md");
        Assert.Contains("apply.ps1", kubernetesReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("kubectl kustomize", kubernetesReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("ClusterIP", kubernetesReadme.Contents, StringComparison.Ordinal);

        var kubernetesApplyScript = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/kubernetes/apply.ps1");
        Assert.Contains("kubectl", kubernetesApplyScript.Contents, StringComparison.Ordinal);
        Assert.Contains("kustomize", kubernetesApplyScript.Contents, StringComparison.Ordinal);
        Assert.Contains("Kubernetes deployment apply completed successfully.", kubernetesApplyScript.Contents, StringComparison.Ordinal);

        var kubernetesKustomization = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/kubernetes/kustomization.yaml");
        Assert.Contains("kind: Kustomization", kubernetesKustomization.Contents, StringComparison.Ordinal);
        Assert.Contains("namespace.yaml", kubernetesKustomization.Contents, StringComparison.Ordinal);
        Assert.Contains("deployment.yaml", kubernetesKustomization.Contents, StringComparison.Ordinal);
        Assert.Contains("service.yaml", kubernetesKustomization.Contents, StringComparison.Ordinal);

        var kubernetesDeployment = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/kubernetes/deployment.yaml");
        Assert.Contains("replace-with-registry/acme-explorer:latest", kubernetesDeployment.Contents, StringComparison.Ordinal);
        Assert.Contains("/health/ready", kubernetesDeployment.Contents, StringComparison.Ordinal);
        Assert.Contains("/health/live", kubernetesDeployment.Contents, StringComparison.Ordinal);

        var kubernetesService = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/kubernetes/service.yaml");
        Assert.Contains("type: ClusterIP", kubernetesService.Contents, StringComparison.Ordinal);
        Assert.Contains("targetPort: http", kubernetesService.Contents, StringComparison.Ordinal);

        var systemdReadme = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/linux/systemd/README.md");
        Assert.Contains("systemd-analyze verify", systemdReadme.Contents, StringComparison.Ordinal);
        Assert.Contains("/opt/Acme.Explorer/current", systemdReadme.Contents, StringComparison.Ordinal);

        var systemdService = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/linux/systemd/Acme.Explorer.service");
        Assert.Contains("EnvironmentFile=-/etc/cephalon/Acme.Explorer.env", systemdService.Contents, StringComparison.Ordinal);
        Assert.Contains("ExecStart=/usr/bin/env dotnet /opt/Acme.Explorer/current/Acme.Explorer.Host.dll", systemdService.Contents, StringComparison.Ordinal);
        Assert.Contains("DynamicUser=true", systemdService.Contents, StringComparison.Ordinal);

        var systemdEnv = Assert.Single(
            scaffold.Files,
            file => file.Path == "deploy/linux/systemd/Acme.Explorer.env");
        Assert.Contains("DOTNET_ENVIRONMENT=Production", systemdEnv.Contents, StringComparison.Ordinal);
        Assert.Contains("Engine__Observability__Telemetry__UseSelfHostedDefaults=true", systemdEnv.Contents, StringComparison.Ordinal);

        var moduleProjectFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Modules.Platform/Acme.Explorer.Modules.Platform.csproj");
        Assert.Contains("<Content Include=\"cephalon.package.json\">", moduleProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", moduleProjectFile.Contents, StringComparison.Ordinal);

        var moduleManifestFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Modules.Platform/cephalon.package.json");
        Assert.Contains("\"id\": \"platform\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"version\": \"9.1.0-preview\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"assembly\": \"Acme.Explorer.Modules.Platform.dll\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"publisher\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"id\": \"acme.explorer\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"displayName\": \"Acme.Explorer\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"minimumEngineVersion\": \"9.1.0-preview\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"supportedTargetFrameworks\": [ \"net10.0\" ]", moduleManifestFile.Contents, StringComparison.Ordinal);

        var hostProjectFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Acme.Explorer.Host.csproj");
        Assert.Contains("Configurations\\**\\*.json", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.OpenTelemetry", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.Serilog", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("Microsoft.Extensions.Hosting.WindowsServices", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("Serilog.Sinks.Console", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("FrameworkReference Include=\"Microsoft.AspNetCore.App\"", hostProjectFile.Contents, StringComparison.Ordinal);

        var hostProgram = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Program.cs");
        Assert.Contains("builder.AddGraphQLTransport();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.AddCephalonSerilog();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.AddCephalonOpenTelemetry();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.Logging.ClearProviders();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("WindowsServiceHelpers.IsWindowsService()", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.Host.UseWindowsService();", hostProgram.Contents, StringComparison.Ordinal);

        var readme = Assert.Single(scaffold.Files, file => file.Path == "README.md");
        Assert.Contains("Engine:Localization", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Agentic Workloads", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Event-Driven Integration", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Edge-Native Delivery", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("ReferenceDocs", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Configurations/[group]/[Environment].json", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("dotnet publish src/Acme.Explorer.Host/Acme.Explorer.Host.csproj -p:PublishProfile=CephalonFolder", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("./artifacts/publish/Acme.Explorer.Host/", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/install-service.ps1", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/install-site.ps1", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service/deploy-zip.ps1", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image/publish-image.ps1", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd/README.md", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd/Acme.Explorer.service", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("docker compose up --build", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("./.cephalon/packages", readme.Contents, StringComparison.Ordinal);

        var dockerfile = Assert.Single(scaffold.Files, file => file.Path == "Dockerfile");
        Assert.Contains("src/Acme.Explorer.Host/Acme.Explorer.Host.csproj", dockerfile.Contents, StringComparison.Ordinal);
        Assert.Contains("Acme.Explorer.Host.dll", dockerfile.Contents, StringComparison.Ordinal);

        var compose = Assert.Single(scaffold.Files, file => file.Path == "compose.yaml");
        Assert.Contains("acme-explorer", compose.Contents, StringComparison.Ordinal);
        Assert.Contains("http://otel-collector:4318", compose.Contents, StringComparison.Ordinal);

        var collectorConfig = Assert.Single(scaffold.Files, file => file.Path == "otel-collector-config.yaml");
        Assert.Contains("health_check", collectorConfig.Contents, StringComparison.Ordinal);
        Assert.Contains("debug", collectorConfig.Contents, StringComparison.Ordinal);

        var dockerignore = Assert.Single(scaffold.Files, file => file.Path == ".dockerignore");
        Assert.Contains("artifacts", dockerignore.Contents, StringComparison.Ordinal);
        Assert.Contains("TestResults", dockerignore.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateAllowsNet11TargetFrameworkOverridesAndAlignsContainerImages()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Readiness",
                modules: ["Operations"],
                targetFramework: "net11.0",
                cephalonPackageVersion: "0.1.0-preview"));

        var hostProject = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Readiness.Host/Acme.Readiness.Host.csproj");
        Assert.Contains("<TargetFramework>net11.0</TargetFramework>", hostProject.Contents, StringComparison.Ordinal);

        var moduleManifest = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Readiness.Modules.Operations/cephalon.package.json");
        Assert.Contains("\"supportedTargetFrameworks\": [ \"net11.0\" ]", moduleManifest.Contents, StringComparison.Ordinal);

        var dockerfile = Assert.Single(scaffold.Files, file => file.Path == "Dockerfile");
        Assert.Contains("FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build", dockerfile.Contents, StringComparison.Ordinal);
        Assert.Contains("FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final", dockerfile.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("mcr.microsoft.com/dotnet/sdk:10.0", dockerfile.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("mcr.microsoft.com/dotnet/aspnet:10.0", dockerfile.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateAddsPhase8StarterConfigPackagesAndRegistrationsWhenSelectionsAreActive()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            patterns: ["CQRS", "Outbox"],
            technologies: ["IdentityAccess", "MultiTenancy", "EventDrivenIntegration"],
            data: new DataSettings(
                readWriteSplit: true,
                outboxEnabled: true,
                idGenerator: "Sfid"),
            identity: new IdentitySettings(
                enabled: true,
                authorizationModes: ["RBAC"]),
            tenancy: new TenancySettings(
                enabled: true,
                mode: "SharedDatabase"),
            audit: new AuditSettings(enabled: true),
            messaging: new MessagingSettings(provider: "Wolverine")));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Platform",
                modules: ["Platform"],
                features: ["Overview"],
                cephalonPackageVersion: "9.1.0-preview"));

        var hostProject = Assert.Single(scaffold.Projects, project => project.Name == "Acme.Platform.Host");
        Assert.Contains("Cephalon.Data", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Ids.Sfid", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Eventing", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Eventing.Wolverine", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Identity", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Identity.AspNetCore", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.MultiTenancy", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Audit", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Observability.Serilog", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Serilog.Sinks.Console", hostProject.Packages, StringComparer.OrdinalIgnoreCase);

        var hostProgram = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Platform.Host/Program.cs");
        Assert.Contains("builder.AddCephalonIdentityAspNetCore();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddData();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddSfidIds();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddEventing();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddWolverineEventing();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddIdentityAccess();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddMultiTenancy();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddAudit();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.Configuration.GetSection(\"Serilog\").Exists()", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.Logging.ClearProviders();", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("builder.AddCephalonSerilog();", hostProgram.Contents, StringComparison.Ordinal);

        var appSettings = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Platform.Host/appsettings.json");
        Assert.Equal("{}", appSettings.Contents.Trim(), ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);

        var appSettingsDevelopment = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Platform.Host/appsettings.Development.json");
        Assert.Equal("{}", appSettingsDevelopment.Contents.Trim(), ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);

        var observabilityDevelopment = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/Observability/Development.json");
        Assert.Contains("\"Serilog\"", observabilityDevelopment.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Serilog.Sinks.Console\"", observabilityDevelopment.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Console\"", observabilityDevelopment.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Application\": \"Acme.Platform.Host\"", observabilityDevelopment.Contents, StringComparison.Ordinal);

        var appModelSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/AddEngine.AppModel.json");
        Assert.Contains("\"Blueprint\": \"modular-monolith\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"cqrs\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"outbox\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"identity-access\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"multi-tenancy\"", appModelSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"event-driven-integration\"", appModelSettings.Contents, StringComparison.Ordinal);

        var dataSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/AddEngine.Data.json");
        Assert.Contains("\"ReadWriteSplit\": true", dataSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Generator\": \"Sfid\"", dataSettings.Contents, StringComparison.Ordinal);

        var identitySettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/AddEngine.Identity.json");
        Assert.Contains("\"AuthorizationModes\": [", identitySettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"RBAC\"", identitySettings.Contents, StringComparison.Ordinal);

        var tenancySettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/AddEngine.Tenancy.json");
        Assert.Contains("\"Mode\": \"SharedDatabase\"", tenancySettings.Contents, StringComparison.Ordinal);

        var auditSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/AddEngine.Audit.json");
        Assert.Contains("\"Audit\": {", auditSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Enabled\": true", auditSettings.Contents, StringComparison.Ordinal);

        var messagingSettings = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Platform.Host/Configurations/AddEngine.Messaging.json");
        Assert.Contains("\"Provider\": \"Wolverine\"", messagingSettings.Contents, StringComparison.Ordinal);

        var compositionSmokeTest = Assert.Single(
            scaffold.Files,
            file => file.Path == "tests/Acme.Platform.Tests/Architecture/CompositionSmokeTests.cs");
        Assert.Contains("namespace Acme.Platform.Tests.Architecture;", compositionSmokeTest.Contents, StringComparison.Ordinal);

        var overviewBehaviorSpecification = Assert.Single(
            scaffold.Files,
            file => file.Path == "tests/Acme.Platform.Tests/Features/OverviewBehaviorSpecifications.cs");
        Assert.Contains("Given_overview_behavior_when_you_start_tdd_then_replace_this_placeholder_with_the_first_failing_specification", overviewBehaviorSpecification.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateAddsCoreBehaviorSpecificationWhenNoFeatureNamesAreProvided()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Core",
                modules: ["Platform"],
                cephalonPackageVersion: "9.1.0-preview"));

        var compositionSmokeTest = Assert.Single(
            scaffold.Files,
            file => file.Path == "tests/Acme.Core.Tests/Architecture/CompositionSmokeTests.cs");
        Assert.Contains("namespace Acme.Core.Tests.Architecture;", compositionSmokeTest.Contents, StringComparison.Ordinal);

        var coreBehaviorSpecification = Assert.Single(
            scaffold.Files,
            file => file.Path == "tests/Acme.Core.Tests/Features/CoreBehaviorSpecifications.cs");
        Assert.Contains("Given_core_behavior_when_you_start_tdd_then_replace_this_placeholder_with_the_first_failing_specification", coreBehaviorSpecification.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateUsesBehaviorBackedRestModulesWhenRestApiTransportIsSelected()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.RestStarter",
                modules: ["Platform"],
                cephalonPackageVersion: "9.1.0-preview"));

        var moduleProject = Assert.Single(scaffold.Projects, project => project.Name == "Acme.RestStarter.Modules.Platform");
        Assert.Contains("Cephalon.Behaviors.Http", moduleProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Behaviors.SourceGen", moduleProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Engine.SourceGen", moduleProject.Packages, StringComparer.OrdinalIgnoreCase);

        var moduleProjectFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.RestStarter.Modules.Platform/Acme.RestStarter.Modules.Platform.csproj");
        Assert.Contains("<PackageReference Include=\"Cephalon.Behaviors.SourceGen\" PrivateAssets=\"all\" />", moduleProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"Cephalon.Engine.SourceGen\" PrivateAssets=\"all\" />", moduleProjectFile.Contents, StringComparison.Ordinal);

        var moduleFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.RestStarter.Modules.Platform/PlatformModule.cs");
        Assert.Contains("RestBehaviorModuleBase", moduleFile.Contents, StringComparison.Ordinal);
        Assert.Contains("ConfigureRestBehaviors", moduleFile.Contents, StringComparison.Ordinal);
        Assert.Contains("MapProfile<GetPlatformStatusBehavior>()", moduleFile.Contents, StringComparison.Ordinal);
        Assert.Contains("[BehaviorRestProfile(BehaviorRestMethod.Get, \"/status\", ApiVersionMajor = 1)]", moduleFile.Contents, StringComparison.Ordinal);

        var hostProgram = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.RestStarter.Host/Program.cs");
        Assert.Contains("using Cephalon.Behaviors.Hosting;", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("using Cephalon.Behaviors.Http.Hosting;", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>", hostProgram.Contents, StringComparison.Ordinal);
        Assert.Contains("behaviors.AddHttpBehaviorBindings();", hostProgram.Contents, StringComparison.Ordinal);

        var readme = Assert.Single(scaffold.Files, file => file.Path == "README.md");
        Assert.Contains("/scalar", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("ConfigureRestBehaviors(...)", readme.Contents, StringComparison.Ordinal);

        var packageProps = Assert.Single(scaffold.Files, file => file.Path == "Directory.Packages.props");
        Assert.Contains("Cephalon.Behaviors.SourceGen", packageProps.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateKeepsGenericModulesWhenRestApiTransportIsNotSelected()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["JsonRpc"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.GenericStarter",
                modules: ["Platform"],
                cephalonPackageVersion: "9.1.0-preview"));

        var moduleProject = Assert.Single(scaffold.Projects, project => project.Name == "Acme.GenericStarter.Modules.Platform");
        Assert.DoesNotContain("Cephalon.Behaviors.Http", moduleProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cephalon.Behaviors.SourceGen", moduleProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Engine.SourceGen", moduleProject.Packages, StringComparer.OrdinalIgnoreCase);

        var moduleFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.GenericStarter.Modules.Platform/PlatformModule.cs");
        Assert.Contains("ModuleBase", moduleFile.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("RestBehaviorModuleBase", moduleFile.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigureRestBehaviors", moduleFile.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateKeepsPackageVersionsAlignedWithRepositoryCatalog()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Catalog",
                modules: ["Platform"],
                cephalonPackageVersion: "2.0.0-preview"));

        var generatedPackages = ParsePackageVersions(
            Assert.Single(scaffold.Files, file => file.Path == "Directory.Packages.props").Contents);
        var repositoryPackages = ParsePackageVersions(File.ReadAllText(FindRepositoryFile("Directory.Packages.props")));

        foreach (var packageId in new[]
                 {
                     "coverlet.collector",
                     "Microsoft.NET.Test.Sdk",
                     "xunit",
                     "xunit.runner.visualstudio"
                 })
        {
            Assert.Equal(repositoryPackages[packageId], generatedPackages[packageId]);
        }
    }

    [Fact]
    public async Task WriteAsyncWritesScaffoldToDiskAndPreventsAccidentalOverwrite()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-scaffold-{Guid.NewGuid():N}");

        try
        {
            var scaffold = ScaffoldGenerator.Generate(
                runtime.Manifest.AppProfile,
                new ScaffoldRequest(
                    appName: "Future.Stack",
                    modules: ["Platform"],
                    cephalonPackageVersion: "1.0.0-preview"));

            await FileSystemScaffoldWriter.WriteAsync(outputPath, scaffold);

            Assert.True(File.Exists(Path.Combine(outputPath, "Future.Stack.slnx")));
            Assert.True(File.Exists(Path.Combine(outputPath, "NuGet.config")));
            Assert.True(File.Exists(Path.Combine(outputPath, ".dockerignore")));
            Assert.True(File.Exists(Path.Combine(outputPath, "Dockerfile")));
            Assert.True(File.Exists(Path.Combine(outputPath, "compose.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "otel-collector-config.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, ".cephalon", "packages", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "windows-service", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "windows-service", "install-service.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "windows-service", "remove-service.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "iis", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "iis", "install-site.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "iis", "remove-site.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "azure-app-service", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "azure-app-service", "deploy-zip.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "container-image", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "container-image", "publish-image.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "apply.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "kustomization.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "namespace.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "deployment.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "service.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "linux", "systemd", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "linux", "systemd", "Future.Stack.service")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "linux", "systemd", "Future.Stack.env")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Future.Stack.Host", "Properties", "PublishProfiles", "CephalonFolder.pubxml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Future.Stack.Host", "Program.cs")));
            Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "Future.Stack.Modules.Platform", "Application")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Future.Stack.Modules.Platform", "cephalon.package.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Future.Stack.Modules.Platform", "PlatformModule.cs")));

            var moduleFileContents = File.ReadAllText(Path.Combine(outputPath, "src", "Future.Stack.Modules.Platform", "PlatformModule.cs"));
            Assert.Contains("RestBehaviorModuleBase", moduleFileContents, StringComparison.Ordinal);
            Assert.Contains("MapProfile<GetPlatformStatusBehavior>()", moduleFileContents, StringComparison.Ordinal);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => FileSystemScaffoldWriter.WriteAsync(outputPath, scaffold));
            Assert.Contains("already exists", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    private static Dictionary<string, string> ParsePackageVersions(string xml)
    {
        var document = XDocument.Parse(xml);

        return document
            .Descendants("PackageVersion")
            .ToDictionary(
                element => element.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException("PackageVersion Include attribute was missing."),
                element => element.Attribute("Version")?.Value
                    ?? throw new InvalidOperationException("PackageVersion Version attribute was missing."),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidatePath = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find '{fileName}' from '{AppContext.BaseDirectory}'.");
    }
}
