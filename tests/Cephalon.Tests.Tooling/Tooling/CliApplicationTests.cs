using Cephalon.Cli;
using Cephalon.Cli.Commands;
using Cephalon.Tests.Support;
using System.Text.Json.Nodes;

namespace Cephalon.Tests.Tooling;

[Collection(ToolingProcessCollectionDefinition.Name)]
public sealed class CliApplicationTests
{
    [Fact]
    public async Task RunAsyncGeneratesAppFromBlueprintCommand()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "new",
                    "Acme.Store",
                    "--blueprint", "Microservice",
                    "--module", "Orders",
                    "--feature", "Checkout",
                    "--transport", "Grpc",
                    "--transport", "JsonRpc",
                    "--technology", "AgenticWorkloads",
                    "--output", outputPath,
                    "--package-version", "3.2.0-preview"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "Acme.Store.slnx")));
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
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "azure-container-apps", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "azure-container-apps", "deploy-up.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "apply.ps1")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "kustomization.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "namespace.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "deployment.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "kubernetes", "service.yaml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "linux", "systemd", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "linux", "systemd", "Acme.Store.service")));
            Assert.True(File.Exists(Path.Combine(outputPath, "deploy", "linux", "systemd", "Acme.Store.env")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Properties", "PublishProfiles", "CephalonFolder.pubxml")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Program.cs")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "appsettings.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "appsettings.Development.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "Observability", "Development.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.AppModel.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Data.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Identity.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Tenancy.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Audit.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Messaging.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Observability.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Localization.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddOpenApi.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddReferenceDocs.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "tests", "Acme.Store.Service.Tests", "Architecture", "CompositionSmokeTests.cs")));
            Assert.True(File.Exists(Path.Combine(outputPath, "tests", "Acme.Store.Service.Tests", "Features", "CheckoutBehaviorSpecifications.cs")));

            var packageProps = await File.ReadAllTextAsync(Path.Combine(outputPath, "Directory.Packages.props"));
            Assert.Contains("Cephalon.AspNetCore.Grpc", packageProps, StringComparison.Ordinal);
            Assert.Contains("Cephalon.Observability.OpenTelemetry", packageProps, StringComparison.Ordinal);
            Assert.Contains("Cephalon.Observability.Serilog", packageProps, StringComparison.Ordinal);
            Assert.Contains("Microsoft.Extensions.Hosting.WindowsServices", packageProps, StringComparison.Ordinal);
            Assert.Contains("Serilog.Sinks.Console", packageProps, StringComparison.Ordinal);
            Assert.Contains("Version=\"3.2.0-preview\"", packageProps, StringComparison.Ordinal);
            Assert.Contains("Version=\"6.1.1\"", packageProps, StringComparison.Ordinal);

            var nuGetConfig = await File.ReadAllTextAsync(Path.Combine(outputPath, "NuGet.config"));
            Assert.Contains("./.cephalon/packages", nuGetConfig, StringComparison.Ordinal);
            Assert.Contains("packageSourceMapping", nuGetConfig, StringComparison.Ordinal);

            var localPackageFeedReadme = await File.ReadAllTextAsync(Path.Combine(outputPath, ".cephalon", "packages", "README.md"));
            Assert.Contains("NuGet.config", localPackageFeedReadme, StringComparison.Ordinal);
            Assert.Contains("publish-package-artifacts.ps1", localPackageFeedReadme, StringComparison.Ordinal);
            Assert.Contains("replace the `cephalon` package source", localPackageFeedReadme, StringComparison.Ordinal);
            Assert.Contains("Dockerfile and compose path use the same restore configuration automatically.", localPackageFeedReadme, StringComparison.Ordinal);

            var publishProfile = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Properties", "PublishProfiles", "CephalonFolder.pubxml"));
            Assert.Contains("PublishDir", publishProfile, StringComparison.Ordinal);
            Assert.Contains("UseAppHost>false", publishProfile, StringComparison.Ordinal);
            Assert.Contains("../../artifacts/publish", publishProfile, StringComparison.Ordinal);

            var windowsInstallScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "windows-service", "install-service.ps1"));
            Assert.Contains("sc.exe create", windowsInstallScript, StringComparison.Ordinal);
            Assert.Contains("--contentRoot", windowsInstallScript, StringComparison.Ordinal);
            Assert.Contains("Acme.Store.Service.dll", windowsInstallScript, StringComparison.Ordinal);

            var windowsRemoveScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "windows-service", "remove-service.ps1"));
            Assert.Contains("Get-Service -Name $ServiceName", windowsRemoveScript, StringComparison.Ordinal);
            Assert.Contains("Stop-Service -Name $ServiceName", windowsRemoveScript, StringComparison.Ordinal);
            Assert.Contains("sc.exe delete $ServiceName", windowsRemoveScript, StringComparison.Ordinal);

            var iisInstallScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "iis", "install-site.ps1"));
            Assert.Contains("add apppool", iisInstallScript, StringComparison.Ordinal);
            Assert.Contains("add site", iisInstallScript, StringComparison.Ordinal);
            Assert.Contains("C:\\inetpub\\sites\\Acme.Store\\current", iisInstallScript, StringComparison.Ordinal);

            var iisRemoveScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "iis", "remove-site.ps1"));
            Assert.Contains("$stopSiteArguments", iisRemoveScript, StringComparison.Ordinal);
            Assert.Contains("$deleteSiteArguments", iisRemoveScript, StringComparison.Ordinal);
            Assert.Contains("$deleteAppPoolArguments", iisRemoveScript, StringComparison.Ordinal);

            var azureAppServiceDeployScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "azure-app-service", "deploy-zip.ps1"));
            Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", azureAppServiceDeployScript, StringComparison.Ordinal);
            Assert.Contains("az @deployArguments", azureAppServiceDeployScript, StringComparison.Ordinal);
            Assert.Contains("azure-app-service.zip", azureAppServiceDeployScript, StringComparison.Ordinal);

            var containerImagePublishScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "container-image", "publish-image.ps1"));
            Assert.Contains("replace-with-registry/acme-store:latest", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("NuGet.config", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Get-DockerBuildArguments", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Format-Command -Command \"docker\"", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("@(\"push\", $tag)", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Push skipped. Re-run with -Push", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Container image publishing completed successfully.", containerImagePublishScript, StringComparison.Ordinal);

            var azureContainerAppsDeployScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "azure-container-apps", "deploy-up.ps1"));
            Assert.Contains("acme-store", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains(@"src\Acme.Store.Service\Acme.Store.Service.csproj", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("NuGet.config", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("az @upArguments", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("--source", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("ASPNETCORE_HTTP_PORTS=8080", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("DOTNET_ENVIRONMENT=Production", azureContainerAppsDeployScript, StringComparison.Ordinal);

            var kubernetesApplyScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "apply.ps1"));
            Assert.Contains("replace-with-registry/acme-store:latest", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("acme-store", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("NuGet.config", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("rendered-manifest.yaml", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("kubectl", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("kustomize", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("Kubernetes deployment apply completed successfully.", kubernetesApplyScript, StringComparison.Ordinal);

            var kubernetesKustomization = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "kustomization.yaml"));
            Assert.Contains("namespace: acme-store", kubernetesKustomization, StringComparison.Ordinal);
            Assert.Contains("namespace.yaml", kubernetesKustomization, StringComparison.Ordinal);
            Assert.Contains("deployment.yaml", kubernetesKustomization, StringComparison.Ordinal);
            Assert.Contains("service.yaml", kubernetesKustomization, StringComparison.Ordinal);

            var kubernetesNamespace = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "namespace.yaml"));
            Assert.Contains("name: acme-store", kubernetesNamespace, StringComparison.Ordinal);
            Assert.Contains("app.kubernetes.io/name: acme-store", kubernetesNamespace, StringComparison.Ordinal);
            Assert.Contains("app.kubernetes.io/part-of: cephalon", kubernetesNamespace, StringComparison.Ordinal);

            var kubernetesDeployment = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "deployment.yaml"));
            Assert.Contains("replace-with-registry/acme-store:latest", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("app.kubernetes.io/component: host", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("imagePullPolicy: IfNotPresent", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("containerPort: 8080", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("ASPNETCORE_HTTP_PORTS", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("DOTNET_ENVIRONMENT", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("/health/ready", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("/health/live", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("startupProbe", kubernetesDeployment, StringComparison.Ordinal);

            var kubernetesService = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "service.yaml"));
            Assert.Contains("type: ClusterIP", kubernetesService, StringComparison.Ordinal);
            Assert.Contains("app.kubernetes.io/component: host", kubernetesService, StringComparison.Ordinal);
            Assert.Contains("targetPort: http", kubernetesService, StringComparison.Ordinal);

            var systemdService = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "linux", "systemd", "Acme.Store.service"));
            Assert.Contains("EnvironmentFile=-/etc/cephalon/Acme.Store.env", systemdService, StringComparison.Ordinal);
            Assert.Contains("ExecStart=/usr/bin/env dotnet /opt/Acme.Store/current/Acme.Store.Service.dll", systemdService, StringComparison.Ordinal);
            Assert.Contains("DynamicUser=true", systemdService, StringComparison.Ordinal);

            var systemdEnvironment = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "linux", "systemd", "Acme.Store.env"));
            Assert.Contains("DOTNET_ENVIRONMENT=Production", systemdEnvironment, StringComparison.Ordinal);
            Assert.Contains("ASPNETCORE_URLS=http://0.0.0.0:8080", systemdEnvironment, StringComparison.Ordinal);
            Assert.Contains("Engine__Observability__Telemetry__Endpoint=http://localhost:4318", systemdEnvironment, StringComparison.Ordinal);

            var generatedReadme = await File.ReadAllTextAsync(Path.Combine(outputPath, "README.md"));
            Assert.Contains("NuGet.config", generatedReadme, StringComparison.Ordinal);
            Assert.Contains("Configurations/Add*.json", generatedReadme, StringComparison.Ordinal);
            Assert.Contains("Configurations/Observability/Development.json", generatedReadme, StringComparison.Ordinal);
            Assert.Contains("CephalonFolder.pubxml", generatedReadme, StringComparison.Ordinal);
            Assert.Contains("deploy/windows-service/README.md", generatedReadme, StringComparison.Ordinal);
            Assert.Contains("deploy/container-image/README.md", generatedReadme, StringComparison.Ordinal);
            Assert.Contains("docker compose up --build", generatedReadme, StringComparison.Ordinal);

            var configurationReadme = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "README.md"));
            Assert.Contains("Configurations/Add*.json", configurationReadme, StringComparison.Ordinal);
            Assert.Contains("Configurations/{group}/{Environment}.json", configurationReadme, StringComparison.Ordinal);
            Assert.Contains("AddCephalonProjectConfigurations()", configurationReadme, StringComparison.Ordinal);

            var windowsServiceReadme = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "windows-service", "README.md"));
            Assert.Contains("install-service.ps1", windowsServiceReadme, StringComparison.Ordinal);
            Assert.Contains("remove-service.ps1", windowsServiceReadme, StringComparison.Ordinal);
            Assert.Contains("CephalonFolder.pubxml", windowsServiceReadme, StringComparison.Ordinal);

            var containerImageReadme = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "container-image", "README.md"));
            Assert.Contains("publish-image.ps1", containerImageReadme, StringComparison.Ordinal);
            Assert.Contains("Dockerfile", containerImageReadme, StringComparison.Ordinal);
            Assert.Contains("docker login", containerImageReadme, StringComparison.Ordinal);

            Assert.Equal("{}", (await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "appsettings.json"))).Trim(), ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);
            Assert.Equal("{}", (await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "appsettings.Development.json"))).Trim(), ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);
            var observabilityDevelopment = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "Observability", "Development.json"));
            Assert.Contains("\"Serilog\"", observabilityDevelopment, StringComparison.Ordinal);
            Assert.Contains("\"Serilog.Sinks.Console\"", observabilityDevelopment, StringComparison.Ordinal);
            Assert.Contains("\"Console\"", observabilityDevelopment, StringComparison.Ordinal);
            Assert.Contains("\"Application\": \"Acme.Store.Service\"", observabilityDevelopment, StringComparison.Ordinal);

            var appModelSettings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.AppModel.json"));
            Assert.Contains("\"microservice\"", appModelSettings, StringComparison.Ordinal);
            Assert.Contains("\"grpc\"", appModelSettings, StringComparison.Ordinal);
            Assert.Contains("\"json-rpc\"", appModelSettings, StringComparison.Ordinal);
            Assert.Contains("\"agentic-workloads\"", appModelSettings, StringComparison.Ordinal);

            var dataSettings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Data.json"));
            Assert.Contains("\"Data\"", dataSettings, StringComparison.Ordinal);

            var identitySettings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Identity.json"));
            Assert.Contains("\"Identity\"", identitySettings, StringComparison.Ordinal);

            var tenancySettings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Tenancy.json"));
            Assert.Contains("\"Tenancy\"", tenancySettings, StringComparison.Ordinal);

            var auditSettings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Audit.json"));
            Assert.Contains("\"Audit\"", auditSettings, StringComparison.Ordinal);

            var observabilitySettings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Configurations", "AddEngine.Observability.json"));
            Assert.DoesNotContain("http://localhost:4317", observabilitySettings, StringComparison.Ordinal);
            Assert.Contains("\"Protocol\": \"otlp/http\"", observabilitySettings, StringComparison.Ordinal);

            var program = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Program.cs"));
            Assert.Contains("builder.AddCephalonProjectConfigurations();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("builder.Configuration.AddEnvironmentVariables();", program, StringComparison.Ordinal);
            Assert.Contains("builder.Configuration.GetSection(\"Serilog\").Exists()", program, StringComparison.Ordinal);
            Assert.Contains("builder.Logging.ClearProviders();", program, StringComparison.Ordinal);
            Assert.Contains("builder.AddCephalonSerilog();", program, StringComparison.Ordinal);
            Assert.Contains("builder.AddCephalonOpenTelemetry();", program, StringComparison.Ordinal);
            Assert.Contains("WindowsServiceHelpers.IsWindowsService()", program, StringComparison.Ordinal);
            Assert.Contains("builder.Host.UseWindowsService();", program, StringComparison.Ordinal);

            var compositionSmokeTest = await File.ReadAllTextAsync(Path.Combine(outputPath, "tests", "Acme.Store.Service.Tests", "Architecture", "CompositionSmokeTests.cs"));
            Assert.Contains("Generated_scaffold_has_a_test_harness_ready_for_real_composition_checks", compositionSmokeTest, StringComparison.Ordinal);

            var checkoutBehaviorSpecification = await File.ReadAllTextAsync(Path.Combine(outputPath, "tests", "Acme.Store.Service.Tests", "Features", "CheckoutBehaviorSpecifications.cs"));
            Assert.Contains("Given_checkout_behavior_when_you_start_tdd_then_replace_this_placeholder_with_the_first_failing_specification", checkoutBehaviorSpecification, StringComparison.Ordinal);

            var compose = await File.ReadAllTextAsync(Path.Combine(outputPath, "compose.yaml"));
            Assert.Contains("http://otel-collector:4318", compose, StringComparison.Ordinal);

            Assert.Contains("Generated 'Acme.Store'", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishesReferenceDocsFromCli()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-reference-docs-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", RepositoryPaths.GetRepositoryRoot(),
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--assembly", "Cephalon.Agentics"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "browse.html")));
            Assert.True(File.Exists(Path.Combine(outputPath, "members.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "reference-manifest.json")));
            Assert.Contains("Published 11 reference doc files", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Assemblies: Cephalon.Engine, Cephalon.Agentics", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorReportsReadyEnvironmentAndTemplateAdvisory()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    9.0.312 [C:\Program Files\dotnet\sdk]
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(103, "No templates found matching: 'cephalon'.", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("[ok] dotnet SDK selection: 10.0.201", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Installed net10.0 SDK family: 10.0.201", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Microsoft.NETCore.App: 10.0.5", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Microsoft.AspNetCore.App: 10.0.5", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Cephalon template pack: No Cephalon templates were found by `dotnet new list cephalon`.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Deployment-mode shipping baseline: Stable shipping floor 'net10.0', readiness lane 'net11.0' (assessment-only).", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Trim support contract: not-claimed. Trimming is not part of the current Cephalon support contract.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Native AOT support contract: not-claimed. Native AOT is not part of the current Cephalon support contract.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Single-file support contract: not-claimed. Single-file publishing is not part of the current Cephalon support contract.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Package-scoped deployment-mode claims:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.Abstractions: singleFile (clean-baseline)", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.Diagnostics: singleFile (clean-baseline)", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.Scaffolding: singleFile (clean-baseline)", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Environment is ready for Cephalon CLI scaffolding.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("cephalon new Acme.Store --output ./Acme.Store", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("dotnet new install Cephalon.TemplatePack", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;
        }
    }

    [Fact]
    public async Task RunAsyncDoctorReportsEngineCompletionScorecardSummary()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 14,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 33,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99,
                "DependencyHealthProviderManifest": {
                  "Reference": "scripts/observability-dependency-health-providers.json",
                  "ManifestSchemaVersion": "1.0.0",
                  "Status": "source-derived-provider-family-contract",
                  "ProviderCount": 18
                }
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 11,
                "TargetDeclaredCount": 11,
                "PendingStableBaselineCount": 1,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 12,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 3,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 104,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 104,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("[ok] Engine completion scorecard artifact: schema 1.24.0 from docs/engine-completion-scorecard.md; conformance matrix docs/conformance-matrix.md.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Engine completion scorecard platform gates: 12 gates; blocked 0, needs-refresh 0, partial 8, not-claimed 1.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Engine completion scorecard evidence references: 33 repo-local references validated by the published artifact.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Engine completion scorecard package GA readiness: 90 package rows; partial 89, not-claimed 1, needs-refresh 0.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Engine completion scorecard deployment-mode evidence: 3 global claims; not-claimed 3, package-scoped claim packages 1, known hazards 14 across 2 packages, transitive audit entries 7, publish probes single-file-publish-gate; claims report artifacts/deployment-mode-claims-release/claim-validation-report.json, gate passed, targets 5, warnings 0, errors 0, truthful package claims 1, boundary audit matched/0, core route-delegate audit matched/0, full common route-delegate audit matched/0, full operator route-delegate audit matched/0, operator response JSON contract audit matched/0, non-operator endpoint audit matched/0, framework endpoint boundary audit matched/0.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Engine completion scorecard adoption smoke evidence: 1 scenario (out-of-tree-generated-app-package-stage, execution-report-ready); runtime probes 6, assertions 7, execution-report fields 9; golden use cases 5, execution-ready 1; report artifacts/adoption-smoke/out-of-tree-package-adoption.json schema 1.0.0.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Engine completion scorecard provider integration evidence: 33 rows; live proofs 33, composition-only 0, external-service gates 14, default-skipped 14, runtime contracts 99; dependency-health providers 18 from scripts/observability-dependency-health-providers.json schema 1.0.0 (source-derived-provider-family-contract).", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Engine completion scorecard eventing operational superiority: contract cephalon-eventing-operational-superiority-promotion-v1 1.0.0; target eventing-operational-superiority; status claimed; required claimed; dimensions 6/6 covered, partial 0, missing 0; coverage 100%; promotion gate allowed; promotion allowed True; decision all-required-dimensions-claimed; runtime concordance matched (19/19 tokens, source src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs); hot-path code-first-publish-subscribe; Wolverine required False.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Engine completion scorecard SRE posture: 11 SLIs; target-declared 11, pending stable baselines 1, stable baselines 10, stable baseline rows 10, stable baseline measurements 12, pending baseline rows 1, blockers 1, pending evidence 1, guardrail-mapped 6, pending guardrail coverage 0, guardrail not-applicable 5, guardrail references 8; stable baseline manifest scripts/sre-stable-baselines.json.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Engine completion scorecard supply-chain release evidence: 12 items; workflow-ready 9, external-policy-pending 3, preflight checks 3, preflight status required-before-real-tag-push, signed-release dry-run blocked/partial, blocker dispatch-identity-actions-disabled, required command pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated, output artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json, handoff artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md, report fields 9, blocked 0.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Engine completion scorecard test coverage evidence: 8 layered projects; gap criteria 4; recommendations 11; shipped 10, gated 1, active gaps 0; quarantine entries 2, open 0, queue empty.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Engine completion scorecard public API compatibility: 104 package baselines; pending packages 0, additions 0, removals 0.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardArtifactIsMissing()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-missing-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard artifact: File", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains(scorecardPath, stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard JSON artifact", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardSchemaIsUnsupported()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-schema-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.0.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 13,
                "PackageGAReadinessCount": 88,
                "PartialPackageGAGates": 87,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard artifact: Unsupported schema '1.0.0'. Doctor expects scorecard schema '1.24.0'.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardDeploymentModeEvidenceDriftsFromSummary()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-deployment-mode-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 13,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 33,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99,
                "DependencyHealthProviderManifest": {
                  "Reference": "scripts/observability-dependency-health-providers.json",
                  "ManifestSchemaVersion": "1.0.0",
                  "Status": "source-derived-provider-family-contract",
                  "ProviderCount": 18
                }
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 11,
                "TargetDeclaredCount": 11,
                "PendingStableBaselineCount": 1,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 12,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 3,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 104,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 104,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard deployment-mode evidence: Artifact", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("deployment-mode summary counts that do not match DeploymentModeEvidence", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardProviderIntegrationEvidenceDriftsFromSummary()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-provider-integration-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 14,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 31,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99,
                "DependencyHealthProviderManifest": {
                  "Reference": "scripts/observability-dependency-health-providers.json",
                  "ManifestSchemaVersion": "1.0.0",
                  "Status": "source-derived-provider-family-contract",
                  "ProviderCount": 18
                }
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 11,
                "TargetDeclaredCount": 11,
                "PendingStableBaselineCount": 1,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 12,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 3,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 104,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 104,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard provider integration evidence: Artifact", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("provider integration summary counts that do not match ProviderIntegrationEvidence", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardProviderIntegrationDependencyHealthManifestIsMissing()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-provider-integration-manifest-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 14,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 33,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 11,
                "TargetDeclaredCount": 11,
                "PendingStableBaselineCount": 1,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 12,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 3,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 104,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 104,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard artifact: Artifact", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("ProviderIntegrationEvidence.DependencyHealthProviderManifest", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardPublicApiEvidenceDriftsFromSummary()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-public-api-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 14,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 33,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99,
                "DependencyHealthProviderManifest": {
                  "Reference": "scripts/observability-dependency-health-providers.json",
                  "ManifestSchemaVersion": "1.0.0",
                  "Status": "source-derived-provider-family-contract",
                  "ProviderCount": 18
                }
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 11,
                "TargetDeclaredCount": 11,
                "PendingStableBaselineCount": 1,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 12,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 3,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 103,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 103,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard public API compatibility: Artifact", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("public API summary counts that do not match PublicApiCompatibilityEvidence", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardSreEvidenceDriftsFromSummary()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-sre-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 14,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 33,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99,
                "DependencyHealthProviderManifest": {
                  "Reference": "scripts/observability-dependency-health-providers.json",
                  "ManifestSchemaVersion": "1.0.0",
                  "Status": "source-derived-provider-family-contract",
                  "ProviderCount": 18
                }
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 10,
                "TargetDeclaredCount": 10,
                "PendingStableBaselineCount": 10,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 12,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 3,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 104,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 104,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard SRE posture: Artifact", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("SRE summary counts that do not match SrePostureEvidence", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenScorecardSupplyChainEvidenceDriftsFromSummary()
    {
        var scorecardPath = Path.Combine(Path.GetTempPath(), $"cephalon-scorecard-supply-chain-{Guid.NewGuid():N}.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        await File.WriteAllTextAsync(scorecardPath, """
            {
              "$schemaVersion": "1.24.0",
              "SourceDocument": "docs/engine-completion-scorecard.md",
              "ConformanceMatrix": "docs/conformance-matrix.md",
              "DeploymentModeEvidence": {
                "GlobalClaimCount": 3,
                "GlobalNotClaimedCount": 3,
                "PackageScopedClaimPackageCount": 1,
                "KnownHazardPackageCount": 2,
                "KnownHazardEntryCount": 14,
                "TransitiveAuditEntryCount": 7,
                "PublishProbeReleaseValidationMode": "single-file-publish-gate",
                "ClaimsReport": "artifacts/deployment-mode-claims-release/claim-validation-report.json",
                "ClaimsReportPresent": true,
                "ClaimsReportPublishProbeGateStatus": "passed",
                "ClaimsReportPublishProbeTargetCount": 5,
                "ClaimsReportPublishProbeWarningCount": 0,
                "ClaimsReportPublishProbeErrorCount": 0,
                "ClaimsReportPackageClaimTruthfulCount": 1,
                "ClaimsReportPackageClaimOverstatedCount": 0,
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus": "matched",
                "ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount": 0,
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus": "matched",
                "ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount": 0,
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus": "matched",
                "ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount": 0,
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus": "matched",
                "ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount": 0,
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus": "matched",
                "ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount": 0
              },
              "AdoptionSmokeEvidence": {
                "ScenarioId": "out-of-tree-generated-app-package-stage",
                "Status": "execution-report-ready",
                "RuntimeProbes": [
                  { "Path": "/engine/packages" },
                  { "Path": "/engine/trust-policy" },
                  { "Path": "/engine/package-policy" },
                  { "Path": "/engine/snapshot" },
                  { "Path": "/engine/runtime-story" },
                  { "Path": "/api/operations/status" }
                ],
                "Assertions": [
                  "runsOutsideRepository",
                  "publishesLocalPackages",
                  "installsCliFromTemporaryFeed",
                  "scaffoldsGeneratedApp",
                  "stagesReferenceModulePackage",
                  "patchesPackagePolicyAndTrust",
                  "runsGeneratedHost"
                ],
                "ExecutionReport": {
                  "DefaultPath": "artifacts/adoption-smoke/out-of-tree-package-adoption.json",
                  "SchemaVersion": "1.0.0",
                  "RequiredFields": [
                    "$schemaVersion",
                    "ScenarioId",
                    "Status",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "DurationMilliseconds",
                    "Assertions",
                    "RuntimeProbes",
                    "Paths"
                  ]
                },
                "GoldenUseCases": [
                  { "Id": "out-of-tree-package-adoption", "Status": "execution-report-ready" },
                  { "Id": "modular-monolith-rest-worker-data", "Status": "planned" },
                  { "Id": "vertical-slice-eventing-outbox", "Status": "planned" },
                  { "Id": "microservice-multi-transport-operations", "Status": "planned" },
                  { "Id": "saas-tenant-governance-audit", "Status": "planned" }
                ]
              },
              "ProviderIntegrationEvidence": {
                "EvidenceRowCount": 33,
                "LiveProofCount": 33,
                "CompositionOnlyCount": 0,
                "ExternalServiceGateCount": 14,
                "DefaultSkippedCount": 14,
                "RuntimeContractCount": 99,
                "DependencyHealthProviderManifest": {
                  "Reference": "scripts/observability-dependency-health-providers.json",
                  "ManifestSchemaVersion": "1.0.0",
                  "Status": "source-derived-provider-family-contract",
                  "ProviderCount": 18
                }
              },
              "EventingOperationalSuperiorityEvidence": {
                "Status": "claimed",
                "RequiredDimensionCount": 6,
                "CoveredDimensionCount": 6,
                "PartialDimensionCount": 0,
                "MissingDimensionCount": 0,
                "CoveragePercent": 100,
                "PromotionGate": "allowed",
                "PromotionAllowed": true,
                "PromotionEvidenceContract": "cephalon-eventing-operational-superiority-promotion-v1",
                "PromotionEvidenceContractVersion": "1.0.0",
                "PromotionTarget": "eventing-operational-superiority",
                "PromotionRequiredStatus": "claimed",
                "PromotionDecisionCode": "all-required-dimensions-claimed",
                "WolverineRequired": false,
                "HotPathBindingMode": "code-first-publish-subscribe",
                "RuntimeConcordanceStatus": "matched",
                "RuntimeConcordanceSource": "src/Cephalon.Eventing/Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs",
                "RuntimeConcordanceTokenCount": 19,
                "RuntimeConcordanceMatchedTokenCount": 19,
                "RuntimeConcordanceMissingTokenCount": 0
              },              "SrePostureEvidence": {
                "SliCount": 11,
                "TargetDeclaredCount": 11,
                "PendingStableBaselineCount": 1,
                "StableBaselineCount": 10,
                "StableBaselineManifest": "scripts/sre-stable-baselines.json",
                "StableBaselineRowCount": 10,
                "StableBaselineMeasurementCount": 12,
                "PendingBaselineRowCount": 1,
                "PendingBaselineBlockerCount": 1,
                "PendingBaselineEvidenceCount": 1,
                "GuardrailMappedSliCount": 6,
                "GuardrailPendingSliCount": 0,
                "GuardrailNotApplicableSliCount": 5,
                "GuardrailReferenceCount": 8
              },
              "SupplyChainEvidence": {
                "EvidenceItemCount": 9,
                "WorkflowReadyCount": 9,
                "ExternalPolicyPendingCount": 2,
                "ExternalPolicyPreflightCheckCount": 3,
                "ExternalPolicyPreflight": {
                  "Status": "required-before-real-tag-push",
                  "RequiredCheckCount": 3
                },
                "SignedReleaseDryRun": {
                  "Status": "blocked",
                  "CurrentProofState": "partial",
                  "CurrentBlockerClass": "dispatch-identity-actions-disabled",
                  "RequiredCommand": "pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated",
                  "OutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json",
                  "HandoffOutputPath": "artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md",
                  "RequiredReportFieldCount": 9
                },
                "BlockedCount": 0
              },
              "TestCoverageEvidence": {
                "LayeredProjectCount": 8,
                "GapDefinitionCriterionCount": 4,
                "RecommendationCount": 11,
                "ShippedRecommendationCount": 10,
                "GatedRecommendationCount": 1,
                "ActiveGapRecommendationCount": 0,
                "QuarantineEntryCount": 2,
                "OpenQuarantineEntryCount": 0,
                "QuarantineQueueStatus": "empty"
              },
              "PublicApiCompatibilityEvidence": {                "PackageCount": 104,
                "PendingPackageCount": 0,
                "HeaderOnlyPackageCount": 104,
                "AdditiveEntryCount": 0,
                "RemovalEntryCount": 0
              },
              "Summary": {
                "PlatformGateCount": 12,
                "BlockedPlatformGates": 0,
                "NeedsRefreshGates": 0,
                "PartialPlatformGates": 8,
                "NotClaimedPlatformGates": 1,
                "EvidenceSourceReferenceCount": 33,
                "PackageGAReadinessCount": 90,
                "PartialPackageGAGates": 89,
                "NotClaimedPackageGAGates": 1,
                "NeedsRefreshPackageGAGates": 0,
                "DeploymentModeGlobalClaimCount": 3,
                "DeploymentModeGlobalNotClaimedCount": 3,
                "DeploymentModePackageScopedClaimPackageCount": 1,
                "DeploymentModeKnownHazardPackageCount": 2,
                "DeploymentModeKnownHazardEntryCount": 14,
                "DeploymentModeTransitiveAuditEntryCount": 7,
                "DeploymentModeClaimsReportPresent": true,
                "DeploymentModeClaimsReportPublishProbeTargetCount": 5,
                "DeploymentModeClaimsReportPublishProbeWarningCount": 0,
                "DeploymentModeClaimsReportPublishProbeErrorCount": 0,
                "DeploymentModeClaimsReportPackageClaimTruthfulCount": 1,
                "DeploymentModeClaimsReportPackageClaimOverstatedCount": 0,
                "DeploymentModeClaimsReportBoundaryAnnotationAuditFailures": 0,
                "DeploymentModeClaimsReportCoreRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures": 0,
                "DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures": 0,
                "DeploymentModeClaimsReportNonOperatorEndpointAuditFailures": 0,
                "DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures": 0,
                "AdoptionSmokeScenarioCount": 1,
                "AdoptionSmokeRuntimeProbeCount": 6,
                "AdoptionSmokeAssertionCount": 7,
                "AdoptionSmokeExecutionReportRequiredFieldCount": 9,
                "AdoptionSmokeGoldenUseCaseCount": 5,
                "AdoptionSmokeGoldenUseCaseExecutionReadyCount": 1,
                "ProviderIntegrationEvidenceRowCount": 33,
                "ProviderIntegrationLiveProofCount": 33,
                "ProviderIntegrationCompositionOnlyCount": 0,
                "ProviderIntegrationExternalServiceGateCount": 14,
                "ProviderIntegrationDefaultSkippedCount": 14,
                "ProviderIntegrationRuntimeContractCount": 99,
                "EventingOperationalSuperiorityRequiredDimensionCount": 6,
                "EventingOperationalSuperiorityCoveredDimensionCount": 6,
                "EventingOperationalSuperiorityPartialDimensionCount": 0,
                "EventingOperationalSuperiorityMissingDimensionCount": 0,
                "EventingOperationalSuperiorityCoveragePercent": 100,
                "EventingOperationalSuperiorityPromotionAllowed": true,
                "EventingOperationalSuperiorityWolverineRequired": false,
                "EventingOperationalSuperiorityRuntimeConcordanceMatched": true,
                "EventingOperationalSuperiorityRuntimeConcordanceTokenCount": 19,
                "EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount": 0,
                "SreSliCount": 11,
                "SreTargetDeclaredCount": 11,
                "SrePendingStableBaselineCount": 1,
                "SreStableBaselineCount": 10,
                "SreGuardrailMappedSliCount": 6,
                "SreGuardrailPendingSliCount": 0,
                "SreGuardrailNotApplicableSliCount": 5,
                "SreGuardrailReferenceCount": 8,
                "SrePendingBaselineRowCount": 1,
                "SrePendingBaselineBlockerCount": 1,
                "SrePendingBaselineEvidenceCount": 1,
                "SupplyChainEvidenceItemCount": 12,
                "SupplyChainWorkflowReadyCount": 9,
                "SupplyChainExternalPolicyPendingCount": 3,
                "SupplyChainExternalPolicyPreflightCheckCount": 3,
                "SupplyChainSignedReleaseDryRunStatus": "blocked",
                "SupplyChainSignedReleaseDryRunBlockerClass": "dispatch-identity-actions-disabled",
                "SupplyChainBlockedCount": 0,
                "TestCoverageLayeredProjectCount": 8,
                "TestCoverageGapCriterionCount": 4,
                "TestCoverageRecommendationCount": 11,
                "TestCoverageShippedRecommendationCount": 10,
                "TestCoverageGatedRecommendationCount": 1,
                "TestCoverageActiveGapRecommendationCount": 0,
                "TestCoverageQuarantineEntryCount": 2,
                "TestCoverageOpenQuarantineEntryCount": 0,
                "PublicApiPackageCount": 104,                "PublicApiPendingPackageCount": 0,
                "PublicApiAdditiveEntryCount": 0,
                "PublicApiRemovalEntryCount": 0
              }
            }
            """);

        UseReadyDoctorProcessRunner();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--scorecard",
                    scorecardPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Engine completion scorecard supply-chain release evidence: Artifact", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("supply-chain summary readback that does not match SupplyChainEvidence", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("scorecard artifact blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (File.Exists(scorecardPath))
            {
                File.Delete(scorecardPath);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorUsesConfiguredTemplateHiveForTemplatePackCommands()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var customHivePath = Path.Combine(Path.GetTempPath(), "cephalon template hive");
        var previousTemplateHivePath = Environment.GetEnvironmentVariable("CEPHALON_DOCTOR_TEMPLATE_HIVE");
        var observedTemplateListCommand = false;

        Environment.SetEnvironmentVariable("CEPHALON_DOCTOR_TEMPLATE_HIVE", customHivePath);
        CommandProcessRunner.RunOverride = (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon", "--debug:custom-hive", var hivePath] when string.Equals(hivePath, customHivePath, StringComparison.Ordinal) => CaptureTemplateListHit(),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });

            CommandProcessResult CaptureTemplateListHit()
            {
                observedTemplateListCommand = true;
                return new CommandProcessResult(103, "No templates found matching: 'cephalon'.", string.Empty);
            }
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(["doctor"], stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.True(observedTemplateListCommand);
            Assert.Contains($"[warn] Cephalon template pack: No Cephalon templates were found by `dotnet new list cephalon --debug:custom-hive \"{customHivePath}\"`.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains($"dotnet new install Cephalon.TemplatePack --debug:custom-hive \"{customHivePath}\"", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains($"dotnet new cephalon-monolith -n Acme.Store.TemplateStarter --debug:custom-hive \"{customHivePath}\"", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("CEPHALON_DOCTOR_TEMPLATE_HIVE", previousTemplateHivePath);
            CommandProcessRunner.RunOverride = null;
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenRequiredSdkSelectionIsTooOld()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "9.0.312", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    9.0.312 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 9.0.14 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 9.0.14 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(103, "No templates found matching: 'cephalon'.", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor"
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] dotnet SDK selection: Current SDK selection is '9.0.312', but Cephalon scaffolds target 'net10.0'.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Installed net10.0 SDK family: No 10.x SDK was found in `dotnet --list-sdks`.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Microsoft.NETCore.App: No 10.x runtime was found in `dotnet --list-runtimes`.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon doctor found 4 required issue(s).", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;
        }
    }

    [Fact]
    public async Task RunAsyncDoctorValidatesGeneratedAppBootstrap()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-app-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(appRootPath, includeLocalPackages: true, includePublishProfile: true);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains($"[ok] Generated app root: {appRootPath}", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app solution: ./Acme.Store.slnx", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated package baseline: Cephalon.AspNetCore 0.1.0-preview, Cephalon.Behaviors.SourceGen 0.1.0-preview, Cephalon.Data 0.1.0-preview, Cephalon.Engine.SourceGen 0.1.0-preview", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Cephalon package source: ./.cephalon/packages", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Cephalon local package feed:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains(".cephalon/packages", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Deployment-mode shipping baseline: Stable shipping floor 'net10.0', readiness lane 'net11.0' (assessment-only).", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Trim support contract: not-claimed. Trimming is not part of the current Cephalon support contract.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated test project: ./tests/Acme.Store.Host.Tests/Acme.Store.Host.Tests.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host target framework: ./src/Acme.Store.Host/Acme.Store.Host.csproj targets net10.0 and stays on the stable shipping floor.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host bootstrap source baseline: ./src/Acme.Store.Host/Program.cs keeps the generated Cephalon host bootstrap explicit with AddCephalonProjectConfigurations, behavior and observability wiring, and MapCephalon().", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated test harness baseline: ./tests/Acme.Store.Host.Tests/Architecture/CompositionSmokeTests.cs plus 1 feature specification placeholder(s) keep the generated composition and Given/When/Then test harness explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host project baseline: ./src/Acme.Store.Host/Acme.Store.Host.csproj keeps the generated package references and `Configurations/**/*.json` copy/publish baseline explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated split configuration assets: ./src/Acme.Store.Host/Configurations/AddEngine.*.json and ./src/Acme.Store.Host/Configurations/Observability/Development.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app-model split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.AppModel.json keeps explicit Engine app-model selections with Blueprint=ModularMonolith and 1 discovery assembly entries.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated engine feature split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Data.json, ./src/Acme.Store.Host/Configurations/AddEngine.Identity.json, ./src/Acme.Store.Host/Configurations/AddEngine.Tenancy.json, ./src/Acme.Store.Host/Configurations/AddEngine.Audit.json, and ./src/Acme.Store.Host/Configurations/AddEngine.Messaging.json keep explicit Engine data, identity, tenancy, audit, and messaging sections with Audit.Enabled=true.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated observability split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Observability.json keeps explicit Engine observability telemetry defaults with Provider=OpenTelemetry, Protocol=otlp/http, ExportLogs=true, ExportMetrics=true, and ExportTraces=true.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated localization split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Localization.json keeps explicit Engine localization defaults with DefaultCulture=en and 2 supported cultures.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated development observability baseline: ./src/Acme.Store.Host/Configurations/Observability/Development.json keeps the generated Serilog console sample explicit with Application=Acme.Store.Host.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated documentation surface assets: ./src/Acme.Store.Host/Configurations/AddOpenApi.json and ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenAPI baseline: ./src/Acme.Store.Host/Configurations/AddOpenApi.json keeps the generated REST docs surface explicit with Title='Acme.Store API'.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated hosted reference docs baseline: ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json keeps hosted reference docs explicit with Enabled=false, RoutePrefix=/reference, DirectoryPath=..\\..\\docs\\reference, and DefaultDocument=browse.html.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local orchestration assets: ./compose.yaml and ./otel-collector-config.yaml are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated compose baseline: ./compose.yaml keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenTelemetry collector baseline: ./otel-collector-config.yaml keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated deployment assets: ./Dockerfile plus container-image, Azure Container Apps, and Kubernetes deployment assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Dockerfile baseline: ./Dockerfile uses sdk:10.0 and aspnet:10.0 for the stable shipping floor.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated container image script baseline: ./deploy/container-image/publish-image.ps1 keeps the generated Dockerfile, NuGet.config, image placeholder, and preview/push flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Azure Container Apps script baseline: ./deploy/azure-container-apps/deploy-up.ps1 keeps the generated source-root, host-project, and az containerapp up defaults explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes apply script baseline: ./deploy/kubernetes/apply.ps1 keeps the generated namespace, image placeholder, manifest root, and kubectl kustomize/apply flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes kustomization baseline: ./deploy/kubernetes/kustomization.yaml keeps the generated namespace plus namespace/deployment/service manifest set explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes namespace baseline: ./deploy/kubernetes/namespace.yaml keeps the generated namespace identity and labels explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes deployment baseline: ./deploy/kubernetes/deployment.yaml keeps the generated container image, env, probe, and resource contract explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes service baseline: ./deploy/kubernetes/service.yaml keeps the generated ClusterIP service contract explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated self-hosted and hosted deployment assets: ./deploy/windows-service, ./deploy/iis, ./deploy/azure-app-service, and ./deploy/linux/systemd assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service baseline: ./deploy/windows-service/install-service.ps1 keeps the generated Windows Service install flow aligned with Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated IIS baseline: ./deploy/iis/install-site.ps1 keeps the generated IIS site/app-pool defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Azure App Service baseline: ./deploy/azure-app-service/deploy-zip.ps1 keeps the generated ZIP package and published host defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Linux systemd baseline: ./deploy/linux/systemd/Acme.Store.service keeps the generated Linux systemd unit aligned with Acme.Store and Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service teardown baseline: ./deploy/windows-service/remove-service.ps1 keeps the generated Windows Service stop/delete flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated IIS teardown baseline: ./deploy/iis/remove-site.ps1 keeps the generated IIS stop/delete flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Linux systemd environment baseline: ./deploy/linux/systemd/Acme.Store.env keeps the generated Linux systemd environment defaults explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated guidance docs assets: ./README.md, ./.cephalon/packages/README.md, ./src/Acme.Store.Host/Configurations/README.md, and deploy/*/README.md guidance assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated root guidance baseline: ./README.md keeps generated package-source, split-config, publish, deployment, and local-orchestration guidance explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local package feed guidance baseline: ./.cephalon/packages/README.md keeps generated local package-feed bootstrap, publish-package-artifacts.ps1, and shared-feed replacement guidance explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated configuration guidance baseline: ./src/Acme.Store.Host/Configurations/README.md keeps generated Add*.json, grouped override, and AddCephalonProjectConfigurations() guidance explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service guide baseline: ./deploy/windows-service/README.md keeps generated Windows Service publish, install, and removal guidance explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated container image guide baseline: ./deploy/container-image/README.md keeps generated Dockerfile build and publish-image.ps1 guidance explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app trim posture: PublishTrimmed is not enabled in the generated app bootstrap.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app Native AOT posture: PublishAot is not enabled in the generated app bootstrap.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app single-file posture: PublishSingleFile is not enabled in the generated app bootstrap.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host project: ./src/Acme.Store.Host/Acme.Store.Host.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated publish profile: ./src/Acme.Store.Host/Properties/PublishProfiles/CephalonFolder.pubxml", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Environment and generated app bootstrap are ready for Cephalon.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains($"Set-Location {QuotePowerShellArgument(appRootPath)}", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("dotnet restore ./Acme.Store.slnx", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("dotnet run --project ./src/Acme.Store.Host/Acme.Store.Host.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorValidatesTemplatePackGeneratedAppBootstrap()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-template-app-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateTemplatePackDoctorAppRoot(appRootPath, "Acme.Store");

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains($"[ok] Generated app root: {appRootPath}", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app solution: Template-pack project-root layout via ./Acme.Store.csproj.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated package baseline: ./Acme.Store.csproj keeps direct Cephalon package versions explicit:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.AspNetCore 0.1.0-preview", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Cephalon package source: ./.cephalon/packages", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Cephalon local package feed: 1 package(s) found under ./.cephalon/packages.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host project: ./Acme.Store.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated test project: Template-pack project-root starters do not emit a default `tests/` project.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host target framework: ./Acme.Store.csproj targets net10.0 and stays on the stable shipping floor.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host bootstrap source baseline: ./Program.cs keeps the generated Cephalon host bootstrap explicit with AddCephalonProjectConfigurations, behavior and observability wiring, and MapCephalon().", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host project baseline: ./Acme.Store.csproj keeps the generated package references and `Configurations/**/*.json` copy/publish baseline explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated split configuration assets: ./Configurations/AddEngine.*.json and ./Configurations/Observability/Development.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local orchestration assets: ./compose.yaml and ./otel-collector-config.yaml are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated deployment assets: ./Dockerfile plus container-image, Azure Container Apps, and Kubernetes deployment assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated self-hosted and hosted deployment assets: ./deploy/windows-service, ./deploy/iis, ./deploy/azure-app-service, and ./deploy/linux/systemd assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated guidance docs assets: ./README.md, ./.cephalon/packages/README.md, ./Configurations/README.md, and deploy/*/README.md guidance assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app trim posture: PublishTrimmed is not enabled in the generated app bootstrap.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app Native AOT posture: PublishAot is not enabled in the generated app bootstrap.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app single-file posture: PublishSingleFile is not enabled in the generated app bootstrap.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated publish profile: ./Properties/PublishProfiles/CephalonFolder.pubxml", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Environment and generated app bootstrap are ready for Cephalon.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains($"Set-Location {QuotePowerShellArgument(appRootPath)}", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("dotnet restore ./Acme.Store.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("dotnet run --project ./Acme.Store.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorWarnsWhenGeneratedAppUsesReadinessLaneAndUnclaimedDeploymentModes()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-readiness-app-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            targetFramework: "net11.0",
            publishTrimmed: true,
            publishAot: true,
            publishSingleFile: true);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("[ok] Generated test project: ./tests/Acme.Store.Host.Tests/Acme.Store.Host.Tests.csproj", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Generated host target framework: ./src/Acme.Store.Host/Acme.Store.Host.csproj targets net11.0 and stays on the assessment-only readiness lane.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host bootstrap source baseline: ./src/Acme.Store.Host/Program.cs keeps the generated Cephalon host bootstrap explicit with AddCephalonProjectConfigurations, behavior and observability wiring, and MapCephalon().", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated test harness baseline: ./tests/Acme.Store.Host.Tests/Architecture/CompositionSmokeTests.cs plus 1 feature specification placeholder(s) keep the generated composition and Given/When/Then test harness explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host project baseline: ./src/Acme.Store.Host/Acme.Store.Host.csproj keeps the generated package references and `Configurations/**/*.json` copy/publish baseline explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated split configuration assets: ./src/Acme.Store.Host/Configurations/AddEngine.*.json and ./src/Acme.Store.Host/Configurations/Observability/Development.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated app-model split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.AppModel.json keeps explicit Engine app-model selections with Blueprint=ModularMonolith and 1 discovery assembly entries.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated engine feature split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Data.json, ./src/Acme.Store.Host/Configurations/AddEngine.Identity.json, ./src/Acme.Store.Host/Configurations/AddEngine.Tenancy.json, ./src/Acme.Store.Host/Configurations/AddEngine.Audit.json, and ./src/Acme.Store.Host/Configurations/AddEngine.Messaging.json keep explicit Engine data, identity, tenancy, audit, and messaging sections with Audit.Enabled=true.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated observability split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Observability.json keeps explicit Engine observability telemetry defaults with Provider=OpenTelemetry, Protocol=otlp/http, ExportLogs=true, ExportMetrics=true, and ExportTraces=true.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated localization split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Localization.json keeps explicit Engine localization defaults with DefaultCulture=en and 2 supported cultures.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated development observability baseline: ./src/Acme.Store.Host/Configurations/Observability/Development.json keeps the generated Serilog console sample explicit with Application=Acme.Store.Host.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated documentation surface assets: ./src/Acme.Store.Host/Configurations/AddOpenApi.json and ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenAPI baseline: ./src/Acme.Store.Host/Configurations/AddOpenApi.json keeps the generated REST docs surface explicit with Title='Acme.Store API'.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated hosted reference docs baseline: ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json keeps hosted reference docs explicit with Enabled=false, RoutePrefix=/reference, DirectoryPath=..\\..\\docs\\reference, and DefaultDocument=browse.html.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local orchestration assets: ./compose.yaml and ./otel-collector-config.yaml are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated compose baseline: ./compose.yaml keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenTelemetry collector baseline: ./otel-collector-config.yaml keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated deployment assets: ./Dockerfile plus container-image, Azure Container Apps, and Kubernetes deployment assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Generated Dockerfile baseline: ./Dockerfile uses sdk:11.0 and aspnet:11.0 for the assessment-only readiness lane.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated container image script baseline: ./deploy/container-image/publish-image.ps1 keeps the generated Dockerfile, NuGet.config, image placeholder, and preview/push flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Azure Container Apps script baseline: ./deploy/azure-container-apps/deploy-up.ps1 keeps the generated source-root, host-project, and az containerapp up defaults explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes apply script baseline: ./deploy/kubernetes/apply.ps1 keeps the generated namespace, image placeholder, manifest root, and kubectl kustomize/apply flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes kustomization baseline: ./deploy/kubernetes/kustomization.yaml keeps the generated namespace plus namespace/deployment/service manifest set explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes namespace baseline: ./deploy/kubernetes/namespace.yaml keeps the generated namespace identity and labels explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes deployment baseline: ./deploy/kubernetes/deployment.yaml keeps the generated container image, env, probe, and resource contract explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Kubernetes service baseline: ./deploy/kubernetes/service.yaml keeps the generated ClusterIP service contract explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated self-hosted and hosted deployment assets: ./deploy/windows-service, ./deploy/iis, ./deploy/azure-app-service, and ./deploy/linux/systemd assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service baseline: ./deploy/windows-service/install-service.ps1 keeps the generated Windows Service install flow aligned with Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated IIS baseline: ./deploy/iis/install-site.ps1 keeps the generated IIS site/app-pool defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Azure App Service baseline: ./deploy/azure-app-service/deploy-zip.ps1 keeps the generated ZIP package and published host defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Linux systemd baseline: ./deploy/linux/systemd/Acme.Store.service keeps the generated Linux systemd unit aligned with Acme.Store and Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service teardown baseline: ./deploy/windows-service/remove-service.ps1 keeps the generated Windows Service stop/delete flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated IIS teardown baseline: ./deploy/iis/remove-site.ps1 keeps the generated IIS stop/delete flow explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Linux systemd environment baseline: ./deploy/linux/systemd/Acme.Store.env keeps the generated Linux systemd environment defaults explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated guidance docs assets: ./README.md, ./.cephalon/packages/README.md, ./src/Acme.Store.Host/Configurations/README.md, and deploy/*/README.md guidance assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated root guidance baseline: ./README.md keeps generated package-source, split-config, publish, deployment, and local-orchestration guidance explicit for Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local package feed guidance baseline: ./.cephalon/packages/README.md keeps generated local package-feed bootstrap, publish-package-artifacts.ps1, and shared-feed replacement guidance explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Generated app trim posture: PublishTrimmed=true in ./src/Acme.Store.Host/Properties/PublishProfiles/CephalonFolder.pubxml, but the support contract remains not-claimed.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Generated app Native AOT posture: PublishAot=true in ./src/Acme.Store.Host/Properties/PublishProfiles/CephalonFolder.pubxml, but the support contract remains not-claimed.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Generated app single-file posture: PublishSingleFile=true in ./src/Acme.Store.Host/Properties/PublishProfiles/CephalonFolder.pubxml, but the support contract remains not-claimed.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Environment and generated app bootstrap are ready for Cephalon.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedGuidanceDocsAssetsAreMissing()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-guides-missing-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            includeGeneratedGuidanceDocsAssets: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated guidance docs assets: Missing generated guidance docs assets:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("./README.md", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("./.cephalon/packages/README.md", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("./src/Acme.Store.Host/Configurations/README.md", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon doctor found 1 required issue(s).", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedRootGuidanceDrifts()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-guides-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            appReadmeContents: """
                # Acme.Store

                This README was rewritten and no longer keeps the generated adoption guidance explicit.
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated root guidance baseline: ./README.md no longer keeps explicit generated guidance for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("NuGet.config", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Configurations/Add*.json", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon doctor found 1 required issue(s).", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedLocalPackageFeedGuidanceDrifts()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-local-feed-guides-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            localPackageFeedReadmeContents: """
                # Local packages

                This file no longer explains how to seed the generated feed.
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated local package feed guidance baseline: ./.cephalon/packages/README.md no longer keeps explicit generated guidance for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("publish-package-artifacts.ps1", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon doctor found 1 required issue(s).", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedAppTargetsFrameworkOutsideSupportContract()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-unsupported-tfm-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            targetFramework: "net8.0");

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated host target framework: ./src/Acme.Store.Host/Acme.Store.Host.csproj targets net8.0, which falls outside the current Cephalon support contract.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedHostBootstrapBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-host-bootstrap-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            programContents: """
                var builder = WebApplication.CreateBuilder(args);
                var app = builder.Build();
                app.Run();
                """,
            projectContents: """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="Cephalon.AspNetCore" Version="0.1.0-preview" />
                  </ItemGroup>

                  <ItemGroup>
                    <Content Include="Configurations\**\*.json">
                      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
                    </Content>
                  </ItemGroup>
                </Project>
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated host bootstrap source baseline:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("AddCephalonProjectConfigurations", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("AddCephalonObservability", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("MapCephalon", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated host project baseline:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.Observability", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.Observability.OpenTelemetry", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Cephalon.Observability.Serilog", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Microsoft.Extensions.Hosting.WindowsServices", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Serilog.Sinks.Console", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("CopyToOutputDirectory=PreserveNewest", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("CopyToPublishDirectory=PreserveNewest", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedTestHarnessBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-test-harness-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            compositionSmokeTestContents: """
                namespace Acme.Store.Host.Tests.Architecture;

                public sealed class CompositionSmokeTests
                {
                }
                """,
            behaviorSpecificationContents: """
                namespace Acme.Store.Host.Tests.Features;

                public sealed class CheckoutBehaviorSpecifications
                {
                    [Fact]
                    public void Placeholder()
                    {
                        Assert.True(true);
                    }
                }
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated test harness baseline: ./tests/Acme.Store.Host.Tests/Architecture/CompositionSmokeTests.cs no longer keeps the generated composition smoke placeholder explicit; ./tests/Acme.Store.Host.Tests/Features/CheckoutBehaviorSpecifications.cs no longer keeps the generated Given/When/Then placeholder explicit.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedAppBootstrapIsIncomplete()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-broken-app-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(appRootPath, includeLocalPackages: false, includePublishProfile: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Cephalon local package feed: No `Cephalon*.nupkg` files were found under", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains(Path.Combine(appRootPath, ".cephalon", "packages"), stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated publish profile: Missing `CephalonFolder.pubxml` for: ./src/Acme.Store.Host/Acme.Store.Host.csproj.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
            Assert.Contains($"cephalon doctor --app-root {QuotePowerShellArgument(appRootPath)}", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedAppDeploymentAssetsAreMissing()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-missing-deploy-assets-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            includeDeploymentAssets: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated deployment assets: Missing generated deployment assets: ./Dockerfile, ./deploy/container-image/publish-image.ps1, ./deploy/azure-container-apps/deploy-up.ps1, ./deploy/kubernetes/apply.ps1, ./deploy/kubernetes/kustomization.yaml, ./deploy/kubernetes/namespace.yaml, ./deploy/kubernetes/deployment.yaml, ./deploy/kubernetes/service.yaml.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedContainerDeploymentScriptBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-container-script-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            containerImagePublishScriptContents: """
                param([string]$Image = "replace-with-registry/acme-store:latest")
                Write-Output $Image
                """,
            azureContainerAppsDeployScriptContents: """
                param([string]$AppName = "acme-store")
                Write-Output $AppName
                """,
            kubernetesApplyScriptContents: """
                param([string]$Namespace = "acme-store")
                Write-Output $Namespace
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated container image script baseline: ./deploy/container-image/publish-image.ps1 no longer keeps the generated deployment-script baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Get-DockerBuildArguments", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Azure Container Apps script baseline: ./deploy/azure-container-apps/deploy-up.ps1 no longer keeps the generated deployment-script baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("az @upArguments", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Kubernetes apply script baseline: ./deploy/kubernetes/apply.ps1 no longer keeps the generated deployment-script baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("rendered-manifest.yaml", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedKubernetesManifestBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-kubernetes-manifest-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            kubernetesKustomizationContents: """
                apiVersion: kustomize.config.k8s.io/v1beta1
                kind: Kustomization
                resources:
                - deployment.yaml
                """,
            kubernetesNamespaceContents: """
                apiVersion: v1
                kind: Namespace
                metadata:
                  name: legacy-store
                """,
            kubernetesDeploymentContents: """
                apiVersion: apps/v1
                kind: Deployment
                metadata:
                  name: legacy-store
                spec:
                  template:
                    spec:
                      containers:
                      - name: legacy-store
                        image: replace-with-registry/legacy-store:stable
                """,
            kubernetesServiceContents: """
                apiVersion: v1
                kind: Service
                metadata:
                  name: legacy-store
                spec:
                  type: NodePort
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated Kubernetes kustomization baseline: ./deploy/kubernetes/kustomization.yaml no longer keeps the generated Kubernetes manifest baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("namespace: acme-store", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Kubernetes namespace baseline: ./deploy/kubernetes/namespace.yaml no longer keeps the generated Kubernetes manifest baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("app.kubernetes.io/part-of: cephalon", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Kubernetes deployment baseline: ./deploy/kubernetes/deployment.yaml no longer keeps the generated Kubernetes manifest baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("ASPNETCORE_HTTP_PORTS", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Kubernetes service baseline: ./deploy/kubernetes/service.yaml no longer keeps the generated Kubernetes manifest baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("targetPort: http", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedLocalOrchestrationAssetsAreMissing()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-missing-local-orchestration-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            includeLocalOrchestrationAssets: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated local orchestration assets: Missing generated local orchestration assets: ./compose.yaml, ./otel-collector-config.yaml.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedSplitConfigurationAssetsAreMissing()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-missing-split-config-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            includeGeneratedSplitConfigurationAssets: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated split configuration assets: Missing generated split configuration assets: ./src/Acme.Store.Host/Configurations/AddEngine.AppModel.json, ./src/Acme.Store.Host/Configurations/AddEngine.Data.json, ./src/Acme.Store.Host/Configurations/AddEngine.Identity.json, ./src/Acme.Store.Host/Configurations/AddEngine.Tenancy.json, ./src/Acme.Store.Host/Configurations/AddEngine.Audit.json, ./src/Acme.Store.Host/Configurations/AddEngine.Messaging.json, ./src/Acme.Store.Host/Configurations/AddEngine.Observability.json, ./src/Acme.Store.Host/Configurations/AddEngine.Localization.json, ./src/Acme.Store.Host/Configurations/Observability/Development.json.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedDocumentationSurfaceAssetsAreMissing()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-missing-doc-surfaces-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            includeDocumentationSurfaceAssets: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated documentation surface assets: Missing generated documentation surface assets: ./src/Acme.Store.Host/Configurations/AddOpenApi.json, ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedPublishedDeploymentAssetsAreMissing()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-missing-published-assets-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            includePublishedDeploymentAssets: false);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated self-hosted and hosted deployment assets: Missing generated self-hosted and hosted deployment assets: ./deploy/windows-service/install-service.ps1, ./deploy/windows-service/remove-service.ps1, ./deploy/iis/install-site.ps1, ./deploy/iis/remove-site.ps1, ./deploy/azure-app-service/deploy-zip.ps1, ./deploy/linux/systemd/Acme.Store.service, ./deploy/linux/systemd/Acme.Store.env.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedSplitConfigurationBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-split-config-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            appModelSettingsContents: """
                {
                  "Engine": {
                    "Blueprint": "ModularMonolith"
                  }
                }
                """,
            dataSettingsContents: """
                {
                  "Engine": {
                    "LegacyData": {
                      "UseSfid": true
                    }
                  }
                }
                """,
            observabilitySettingsContents: """
                {
                  "Engine": {
                    "Observability": {
                      "LogManifestSummary": true,
                      "LogModuleSummary": true,
                      "LogCapabilitySummary": true,
                      "Telemetry": {
                        "Provider": "",
                        "Protocol": "otlp/http",
                        "ExportLogs": true,
                        "ExportMetrics": true,
                        "ExportTraces": true
                      }
                    }
                  }
                }
                """,
            localizationSettingsContents: """
                {
                  "Engine": {
                    "Localization": {
                      "DefaultCulture": "en",
                      "SupportedCultures": [
                        "en",
                        "th"
                      ],
                      "Resources": {
                        "en": {
                          "engine.docs.rest.title": "Acme.Store API"
                        }
                      }
                    }
                  }
                }
                """,
            developmentObservabilitySettingsContents: """
                {
                  "Serilog": {
                    "Using": [
                      "Serilog.Sinks.Console"
                    ],
                    "MinimumLevel": {
                      "Default": "Information",
                      "Override": {
                        "Microsoft": "Warning",
                        "System": "Warning"
                      }
                    },
                    "WriteTo": [
                      {
                        "Name": "File"
                      }
                    ],
                    "Properties": {
                      "Application": ""
                    }
                  }
                }
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated app-model split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.AppModel.json no longer keeps explicit Engine blueprint, discovery assemblies, pattern, technology, and transport selections.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated engine feature split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Data.json no longer keeps an explicit `Engine:Data` section.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated observability split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Observability.json no longer keeps explicit Engine observability summary and telemetry export settings.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated localization split-config baseline: ./src/Acme.Store.Host/Configurations/AddEngine.Localization.json no longer keeps explicit Engine localization culture and resource defaults.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated development observability baseline: ./src/Acme.Store.Host/Configurations/Observability/Development.json no longer keeps the generated Serilog console sample explicit for development overrides.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedDocumentationSurfaceBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-doc-surface-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            openApiSettingsContents: """
                {
                  "OpenApi": {
                    "Title": ""
                  }
                }
                """,
            referenceDocsSettingsContents: """
                {
                  "ReferenceDocs": {
                    "Enabled": "legacy",
                    "RoutePrefix": "",
                    "DirectoryPath": "",
                    "DefaultDocument": ""
                  }
                }
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated OpenAPI baseline: ./src/Acme.Store.Host/Configurations/AddOpenApi.json no longer keeps an explicit `OpenApi:Title` for the generated REST docs surface.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated hosted reference docs baseline: ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json no longer keeps an explicit boolean `ReferenceDocs:Enabled` setting.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedDockerfileDriftsFromHostTargetFramework()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-dockerfile-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            dockerSdkImageTag: "11.0",
            dockerAspNetImageTag: "11.0");

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated Dockerfile baseline: ./Dockerfile uses sdk:11.0 and aspnet:11.0, but ./src/Acme.Store.Host/Acme.Store.Host.csproj targets net10.0.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedLocalOrchestrationBaselinesDrift()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-local-orchestration-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            composeFileContents: """
                services:
                  acme-store:
                    build:
                      context: .
                    environment:
                      DOTNET_ENVIRONMENT: Production
                      Engine__Observability__Telemetry__Endpoint: http://legacy-collector:4318
                  otel-collector:
                    image: otel/opentelemetry-collector-contrib:0.149.0
                """,
            otelCollectorConfigContents: """
                receivers:
                  otlp:
                    protocols:
                      grpc:
                        endpoint: 0.0.0.0:4317

                exporters:
                  logging: {}
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated compose baseline: ./compose.yaml no longer keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated OpenTelemetry collector baseline: ./otel-collector-config.yaml no longer keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorFailsWhenGeneratedPublishedDeploymentBaselinesDriftFromHostIdentity()
    {
        var appRootPath = Path.Combine(Path.GetTempPath(), $"cephalon-doctor-published-asset-drift-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        CreateGeneratedDoctorAppRoot(
            appRootPath,
            includeLocalPackages: true,
            includePublishProfile: true,
            windowsServiceInstallScriptContents: """
                sc.exe create Acme.Store binPath= "dotnet Legacy.Store.Host.dll"
                """,
            windowsServiceRemoveScriptContents: """
                Write-Output "legacy remove"
                """,
            iisInstallScriptContents: """
                $siteName = "Legacy.Store"
                Write-Output $siteName
                """,
            iisRemoveScriptContents: """
                Write-Output "legacy teardown"
                """,
            azureAppServiceDeployScriptContents: """
                $appName = "Legacy.Store"
                Write-Output $appName
                """,
            linuxSystemdServiceContents: """
                [Unit]
                Description=Legacy Store

                [Service]
                WorkingDirectory=/opt/Legacy.Store/current
                ExecStart=/usr/bin/dotnet /opt/Legacy.Store/current/Legacy.Store.Host.dll

                [Install]
                WantedBy=multi-user.target
                """,
            linuxSystemdEnvironmentContents: """
                ASPNETCORE_URLS=http://127.0.0.1:5000
                """);

        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(0, "cephalon-monolith", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "doctor",
                    "--app-root", appRootPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("[error] Generated Windows Service baseline: ./deploy/windows-service/install-service.ps1 no longer references Acme.Store.Host.dll through the generated Windows Service install flow.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated IIS baseline: ./deploy/iis/install-site.ps1 no longer keeps the generated IIS site/app-pool defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Azure App Service baseline: ./deploy/azure-app-service/deploy-zip.ps1 no longer keeps the generated ZIP package and published host defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Linux systemd baseline: ./deploy/linux/systemd/Acme.Store.service no longer keeps the generated Linux systemd unit aligned with Acme.Store and Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Windows Service teardown baseline: ./deploy/windows-service/remove-service.ps1 no longer keeps the generated deployment-script baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated IIS teardown baseline: ./deploy/iis/remove-site.ps1 no longer keeps the generated deployment-script baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[error] Generated Linux systemd environment baseline: ./deploy/linux/systemd/Acme.Store.env no longer keeps the generated Linux systemd environment baseline explicit for:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("generated-app bootstrap blockers", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CommandProcessRunner.RunOverride = null;

            if (Directory.Exists(appRootPath))
            {
                Directory.Delete(appRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncDoctorRequiresAppRootValue()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "doctor",
                "--app-root"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Option '--app-root' requires a value.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncDoctorRequiresScorecardValue()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "doctor",
                "--scorecard"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Option '--scorecard' requires a value.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncStagesPublishedModulePackageIntoLoadableDirectory()
    {
        var packageOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-package-stage-pack-{Guid.NewGuid():N}");
        var stagedOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-package-stage-output-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(packageOutputPath);

        try
        {
            var packagePath = PackReferenceModulePackage(packageOutputPath);

            var exitCode = await CliApplication.RunAsync(
                [
                    "package",
                    "stage",
                    "--package", packagePath,
                    "--output", stagedOutputPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(stagedOutputPath, "cephalon.package.json")));
            Assert.True(File.Exists(Path.Combine(stagedOutputPath, "Cephalon.ReferenceModule.Operations.dll")));
            Assert.True(File.Exists(Path.Combine(stagedOutputPath, "Cephalon.ReferenceModule.Operations.xml")));
            Assert.True(File.Exists(Path.Combine(stagedOutputPath, "PACKAGE.md")));

            var manifest = await LoadJsonObjectAsync(Path.Combine(stagedOutputPath, "cephalon.package.json"));
            Assert.Equal("reference-operations", manifest["id"]?.GetValue<string>());
            Assert.Equal("Cephalon.ReferenceModule.Operations.dll", manifest["assembly"]?.GetValue<string>());
            Assert.Contains("Staged package 'reference-operations'", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Next step: point Engine:Discovery:PackageDirectories", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(packageOutputPath))
            {
                Directory.Delete(packageOutputPath, recursive: true);
            }

            if (Directory.Exists(stagedOutputPath))
            {
                Directory.Delete(stagedOutputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishesReferenceDocsAndEnablesHostingFromCli()
    {
        var rootPath = RepositoryPaths.GetRepositoryRoot();
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-publish-hosting-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(workspacePath, "published-docs");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", rootPath,
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--enable-hosting",
                    "--appsettings", appSettingsPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "browse.html")));

            var rootObject = await LoadJsonObjectAsync(appSettingsPath);
            var referenceDocs = Assert.IsType<JsonObject>(rootObject["ReferenceDocs"]);

            Assert.Equal(true, referenceDocs["Enabled"]?.GetValue<bool>());
            Assert.Equal("/reference", referenceDocs["RoutePrefix"]?.GetValue<string>());
            Assert.Equal("..\\..\\published-docs", referenceDocs["DirectoryPath"]?.GetValue<string>());
            Assert.Equal("browse.html", referenceDocs["DefaultDocument"]?.GetValue<string>());
            Assert.Contains("Published", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Enabled hosted reference docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishesReferenceDocsAndValidatesHostingFromCli()
    {
        var rootPath = RepositoryPaths.GetRepositoryRoot();
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-publish-validate-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(workspacePath, "published-docs");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", rootPath,
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--enable-hosting",
                    "--validate-hosting",
                    "--appsettings", appSettingsPath,
                    "--host-url", "https://localhost:7235"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("Published", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Enabled hosted reference docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("ReferenceDocs hosting is ready", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Hosted browser URL: https://localhost:7235/reference/browse.html", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishOpenUsesGeneratedBrowseHtml()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-reference-docs-open-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Uri? launchedUri = null;

        DocumentationLauncher.OpenOverride = (target, _) =>
        {
            launchedUri = target;
            return Task.CompletedTask;
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", RepositoryPaths.GetRepositoryRoot(),
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--open"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.NotNull(launchedUri);
            Assert.True(launchedUri!.IsFile);
            Assert.EndsWith("browse.html", launchedUri.LocalPath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Opened reference docs:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            DocumentationLauncher.OpenOverride = null;

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishOpenUsesHostedUrlWhenRequested()
    {
        var rootPath = RepositoryPaths.GetRepositoryRoot();
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-open-hosted-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(workspacePath, "published-docs");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Uri? launchedUri = null;

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        DocumentationLauncher.OpenOverride = (target, _) =>
        {
            launchedUri = target;
            return Task.CompletedTask;
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", rootPath,
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--enable-hosting",
                    "--appsettings", appSettingsPath,
                    "--open",
                    "--host-url", "https://localhost:7235"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.NotNull(launchedUri);
            Assert.Equal("https://localhost:7235/reference/browse.html", launchedUri!.AbsoluteUri);
            Assert.Contains("Opened reference docs: https://localhost:7235/reference/browse.html", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            DocumentationLauncher.OpenOverride = null;

            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncEnablesHostedReferenceDocsFromCli()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-enable-hosting-{Guid.NewGuid():N}");
        var appSettingsDirectory = Path.Combine(rootPath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        Directory.CreateDirectory(Path.Combine(rootPath, "docs", "reference"));
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "enable-hosting",
                    "--appsettings", appSettingsPath,
                    "--root", rootPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);

            var rootObject = await LoadJsonObjectAsync(appSettingsPath);
            var referenceDocs = Assert.IsType<JsonObject>(rootObject["ReferenceDocs"]);

            Assert.Equal(true, referenceDocs["Enabled"]?.GetValue<bool>());
            Assert.Equal("/reference", referenceDocs["RoutePrefix"]?.GetValue<string>());
            Assert.Equal("..\\..\\docs\\reference", referenceDocs["DirectoryPath"]?.GetValue<string>());
            Assert.Equal("browse.html", referenceDocs["DefaultDocument"]?.GetValue<string>());
            Assert.Contains("Enabled hosted reference docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncEnableHostingPreservesExistingSettingsUnlessOverridden()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-preserve-hosting-{Guid.NewGuid():N}");
        var appSettingsDirectory = Path.Combine(rootPath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "ReferenceDocs": {
                "Enabled": false,
                "RoutePrefix": "/docs",
                "DirectoryPath": "..\\..\\custom-reference",
                "DefaultDocument": "landing.html"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "enable-hosting",
                    "--appsettings", appSettingsPath,
                    "--root", rootPath,
                    "--default-document", "browse.html"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);

            var rootObject = await LoadJsonObjectAsync(appSettingsPath);
            var referenceDocs = Assert.IsType<JsonObject>(rootObject["ReferenceDocs"]);

            Assert.Equal(true, referenceDocs["Enabled"]?.GetValue<bool>());
            Assert.Equal("/docs", referenceDocs["RoutePrefix"]?.GetValue<string>());
            Assert.Equal("..\\..\\custom-reference", referenceDocs["DirectoryPath"]?.GetValue<string>());
            Assert.Equal("browse.html", referenceDocs["DefaultDocument"]?.GetValue<string>());
            Assert.Contains("DefaultDocument='browse.html'", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishEnableHostingRequiresAppSettings()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "docs",
                "publish",
                "--root", RepositoryPaths.GetRepositoryRoot(),
                "--enable-hosting"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Option '--enable-hosting' requires '--appsettings <path>'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncPublishValidateHostingRequiresAppSettings()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "docs",
                "publish",
                "--root", RepositoryPaths.GetRepositoryRoot(),
                "--validate-hosting"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Option '--validate-hosting' requires '--appsettings <path>'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncPublishHostUrlRequiresOpen()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "docs",
                "publish",
                "--root", RepositoryPaths.GetRepositoryRoot(),
                "--enable-hosting",
                "--appsettings", "appsettings.json",
                "--host-url", "https://localhost:7235"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Option '--host-url' requires '--open' or '--validate-hosting'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncValidateHostingSucceedsWhenReferenceDocsAreReady()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-validate-hosting-{Guid.NewGuid():N}");
        var docsDirectory = Path.Combine(workspacePath, "docs", "reference");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(docsDirectory);
        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(Path.Combine(docsDirectory, "browse.html"), "<html></html>");
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "ReferenceDocs": {
                "Enabled": true,
                "RoutePrefix": "/reference",
                "DirectoryPath": "..\\..\\docs\\reference",
                "DefaultDocument": "browse.html"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "validate-hosting",
                    "--appsettings", appSettingsPath,
                    "--host-url", "https://localhost:7235"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("ReferenceDocs hosting is ready", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Hosted browser URL: https://localhost:7235/reference/browse.html", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Engine reference-docs URL: https://localhost:7235/engine/reference-docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncValidateHostingFailsWhenDefaultDocumentIsMissing()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-validate-hosting-missing-{Guid.NewGuid():N}");
        var docsDirectory = Path.Combine(workspacePath, "docs", "reference");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(docsDirectory);
        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "ReferenceDocs": {
                "Enabled": true,
                "RoutePrefix": "/reference",
                "DirectoryPath": "..\\..\\docs\\reference",
                "DefaultDocument": "browse.html"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "validate-hosting",
                    "--appsettings", appSettingsPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("does not exist", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncShowsErrorForUnknownOption()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["new", "Acme.Store", "--unknown"],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown option '--unknown'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncHelpIncludesDoctorCommand()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["--help"],
            stdout,
            stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("cephalon doctor [options]", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("--app-root <path>", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("--scorecard <path>", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Doctor options:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Package stage options:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr.ToString());
    }

    private static void UseReadyDoctorProcessRunner()
    {
        CommandProcessRunner.RunOverride = static (fileName, arguments, _, _) =>
        {
            Assert.Equal("dotnet", fileName);

            return Task.FromResult(arguments switch
            {
                ["--version"] => new CommandProcessResult(0, "10.0.201", string.Empty),
                ["--list-sdks"] => new CommandProcessResult(0, """
                    10.0.201 [C:\Program Files\dotnet\sdk]
                    """, string.Empty),
                ["--list-runtimes"] => new CommandProcessResult(0, """
                    Microsoft.AspNetCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
                    Microsoft.NETCore.App 10.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    """, string.Empty),
                ["new", "list", "cephalon"] => new CommandProcessResult(103, "No templates found matching: 'cephalon'.", string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {fileName} {string.Join(' ', arguments)}")
            });
        };
    }

    private static void CreateGeneratedDoctorAppRoot(
        string appRootPath,
        bool includeLocalPackages,
        bool includePublishProfile,
        string targetFramework = "net10.0",
        bool publishTrimmed = false,
        bool publishAot = false,
        bool publishSingleFile = false,
        bool includeGeneratedTestHarnessAssets = true,
        bool includeGeneratedSplitConfigurationAssets = true,
        bool includeDocumentationSurfaceAssets = true,
        bool includeDeploymentAssets = true,
        bool includePublishedDeploymentAssets = true,
        bool includeLocalOrchestrationAssets = true,
        string? projectContents = null,
        string? programContents = null,
        string? testProjectContents = null,
        string? compositionSmokeTestContents = null,
        string? behaviorSpecificationContents = null,
        string? appModelSettingsContents = null,
        string? dataSettingsContents = null,
        string? identitySettingsContents = null,
        string? tenancySettingsContents = null,
        string? auditSettingsContents = null,
        string? messagingSettingsContents = null,
        string? observabilitySettingsContents = null,
        string? localizationSettingsContents = null,
        string? developmentObservabilitySettingsContents = null,
        string? openApiSettingsContents = null,
        string? referenceDocsSettingsContents = null,
        string? composeFileContents = null,
        string? otelCollectorConfigContents = null,
        string? dockerSdkImageTag = null,
        string? dockerAspNetImageTag = null,
        string? containerImagePublishScriptContents = null,
        string? azureContainerAppsDeployScriptContents = null,
        string? kubernetesApplyScriptContents = null,
        string? kubernetesKustomizationContents = null,
        string? kubernetesNamespaceContents = null,
        string? kubernetesDeploymentContents = null,
        string? kubernetesServiceContents = null,
        string? windowsServiceInstallScriptContents = null,
        string? windowsServiceRemoveScriptContents = null,
        string? iisInstallScriptContents = null,
        string? iisRemoveScriptContents = null,
        string? azureAppServiceDeployScriptContents = null,
        string? linuxSystemdServiceContents = null,
        string? linuxSystemdEnvironmentContents = null,
        bool includeGeneratedGuidanceDocsAssets = true,
        string? appReadmeContents = null,
        string? localPackageFeedReadmeContents = null,
        string? configurationReadmeContents = null,
        string? windowsServiceGuideContents = null,
        string? iisGuideContents = null,
        string? azureAppServiceGuideContents = null,
        string? containerImageGuideContents = null,
        string? azureContainerAppsGuideContents = null,
        string? kubernetesGuideContents = null,
        string? linuxSystemdGuideContents = null)
    {
        Directory.CreateDirectory(appRootPath);
        Directory.CreateDirectory(Path.Combine(appRootPath, ".cephalon", "packages"));
        Directory.CreateDirectory(Path.Combine(appRootPath, "src", "Acme.Store.Host"));

        File.WriteAllText(Path.Combine(appRootPath, "Acme.Store.slnx"), "<Solution />");
        File.WriteAllText(
            Path.Combine(appRootPath, "Directory.Packages.props"),
            """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Cephalon.AspNetCore" Version="0.1.0-preview" />
                <PackageVersion Include="Cephalon.Behaviors.SourceGen" Version="0.1.0-preview" />
                <PackageVersion Include="Cephalon.Data" Version="0.1.0-preview" />
                <PackageVersion Include="Cephalon.Engine.SourceGen" Version="0.1.0-preview" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(appRootPath, "NuGet.config"),
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="cephalon" value="./.cephalon/packages" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="cephalon">
                  <package pattern="Cephalon*" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """);

        var localPackageFeedPath = Path.Combine(appRootPath, ".cephalon", "packages");
        if (includeLocalPackages)
        {
            File.WriteAllBytes(Path.Combine(localPackageFeedPath, "Cephalon.AspNetCore.0.1.0-preview.nupkg"), []);
        }

        var hostProjectPath = Path.Combine(appRootPath, "src", "Acme.Store.Host", "Acme.Store.Host.csproj");
        File.WriteAllText(
            hostProjectPath,
            projectContents ?? $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{targetFramework}}</TargetFramework>
                <Description>Cephalon modular monolith app generated from dotnet new.</Description>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Cephalon.AspNetCore" Version="0.1.0-preview" />
                <PackageReference Include="Cephalon.Behaviors.SourceGen" Version="0.1.0-preview" PrivateAssets="all" />
                <PackageReference Include="Cephalon.Engine.SourceGen" Version="0.1.0-preview" PrivateAssets="all" />
                <PackageReference Include="Cephalon.Observability" Version="0.1.0-preview" />
                <PackageReference Include="Cephalon.Observability.OpenTelemetry" Version="0.1.0-preview" />
                <PackageReference Include="Cephalon.Observability.Serilog" Version="0.1.0-preview" />
                <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.5" />
                <PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
              </ItemGroup>

              <ItemGroup>
                <Content Update="Configurations\**\*.json">
                  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
                  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(appRootPath, "src", "Acme.Store.Host", "Program.cs"),
            programContents ?? """
            using Cephalon.AspNetCore.Hosting;
            using Cephalon.Behaviors.Hosting;
            using Cephalon.Behaviors.Http.Hosting;
            using Cephalon.Observability.Hosting;
            using Cephalon.Observability.OpenTelemetry.Hosting;
            using Cephalon.Observability.Serilog.Hosting;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.Hosting.WindowsServices;

            var options = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService()
                    ? AppContext.BaseDirectory
                    : default
            };

            var builder = WebApplication.CreateBuilder(options);
            builder.AddCephalonProjectConfigurations();
            builder.Host.UseWindowsService();
            builder.AddCephalon(engine =>
            {
                engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
                {
                    behaviors.AddHttpBehaviorBindings();
                });
            });
            builder.Services.AddCephalonObservability(builder.Configuration);
            if (builder.Configuration.GetSection("Serilog").Exists())
            {
                builder.Logging.ClearProviders();
            }
            builder.AddCephalonSerilog();
            builder.AddCephalonOpenTelemetry();

            var app = builder.Build();

            app.MapGet("/", () => TypedResults.Ok(new
            {
                name = "Acme.Store.Host",
                blueprint = "ModularMonolith"
            })).ExcludeFromDescription();

            app.MapCephalon();
            app.Run();
            """);
        File.WriteAllText(Path.Combine(appRootPath, "src", "Acme.Store.Host", "appsettings.json"), "{}");

        if (includeGeneratedTestHarnessAssets)
        {
            var testProjectRoot = Path.Combine(appRootPath, "tests", "Acme.Store.Host.Tests");
            Directory.CreateDirectory(Path.Combine(testProjectRoot, "Architecture"));
            Directory.CreateDirectory(Path.Combine(testProjectRoot, "Features"));

            File.WriteAllText(
                Path.Combine(testProjectRoot, "Acme.Store.Host.Tests.csproj"),
                testProjectContents ?? $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{{targetFramework}}</TargetFramework>
                    <IsPackable>false</IsPackable>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(testProjectRoot, "Architecture", "CompositionSmokeTests.cs"),
                compositionSmokeTestContents ?? """
                using Xunit;

                namespace Acme.Store.Host.Tests.Architecture;

                public sealed class CompositionSmokeTests
                {
                    [Fact]
                    public void Generated_scaffold_has_a_test_harness_ready_for_real_composition_checks()
                    {
                        Assert.True(true);
                    }
                }
                """);
            File.WriteAllText(
                Path.Combine(testProjectRoot, "Features", "CheckoutBehaviorSpecifications.cs"),
                behaviorSpecificationContents ?? """
                using Xunit;

                namespace Acme.Store.Host.Tests.Features;

                public sealed class CheckoutBehaviorSpecifications
                {
                    [Fact]
                    public void Given_checkout_behavior_when_you_start_tdd_then_replace_this_placeholder_with_the_first_failing_specification()
                    {
                        Assert.True(false, "Replace this placeholder with the first failing behavior specification.");
                    }
                }
                """);
        }

        var configurationsPath = Path.Combine(appRootPath, "src", "Acme.Store.Host", "Configurations");

        if (includeGeneratedSplitConfigurationAssets || includeDocumentationSurfaceAssets)
        {
            Directory.CreateDirectory(configurationsPath);
        }

        if (includeGeneratedGuidanceDocsAssets)
        {
            File.WriteAllText(
                Path.Combine(appRootPath, "README.md"),
                appReadmeContents ?? """
                # Acme.Store

                Generated app guidance for Acme.Store.

                - NuGet.config
                - .cephalon/packages
                - Configurations/Add*.json
                - Configurations/Observability/Development.json
                - CephalonFolder.pubxml
                - deploy/windows-service/README.md
                - deploy/iis/README.md
                - deploy/azure-app-service/README.md
                - deploy/container-image/README.md
                - deploy/azure-container-apps/README.md
                - deploy/kubernetes/README.md
                - deploy/linux/systemd/README.md

                Run docker compose up --build after packages are reachable, then inspect /engine/snapshot.
                """);
            File.WriteAllText(
                Path.Combine(localPackageFeedPath, "README.md"),
                localPackageFeedReadmeContents ?? """
                # Cephalon local package feed

                `NuGet.config` points `Cephalon*` package restore at this directory by default so generated apps can build before you publish Cephalon packages to a shared feed.

                Populate this directory from the Cephalon repository with:

                ```powershell
                pwsh <path-to-cephalon-repo>/scripts/publish-package-artifacts.ps1 -OutputPath <absolute-path-to-this-folder>
                ```

                If your team already publishes Cephalon packages to a shared source, replace the `cephalon` package source in `NuGet.config` instead. The Dockerfile and compose path use the same restore configuration automatically.
                """);

            Directory.CreateDirectory(configurationsPath);
            File.WriteAllText(
                Path.Combine(configurationsPath, "README.md"),
                configurationReadmeContents ?? """
                # Host Configuration

                Keep Cephalon defaults in Configurations/Add*.json.
                Add grouped overrides under Configurations/{group}/{Environment}.json.
                Keep appsettings.json and appsettings.{Environment}.json for project overrides.
                Configurations/Observability/Development.json already seeds the development override.
                Program.cs loads this through AddCephalonProjectConfigurations().
                """);
        }

        if (includeGeneratedSplitConfigurationAssets)
        {
            Directory.CreateDirectory(Path.Combine(configurationsPath, "Observability"));
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.AppModel.json"),
                appModelSettingsContents ?? """
                {
                  "Engine": {
                    "Blueprint": "ModularMonolith",
                    "Discovery": {
                      "Assemblies": [
                        "Acme.Store.Modules.Catalog"
                      ]
                    },
                    "Patterns": [
                      "SharedFoundation"
                    ],
                    "Technologies": [
                      "Data"
                    ],
                    "Transports": [
                      "RestApi"
                    ]
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Data.json"),
                dataSettingsContents ?? """
                {
                  "Engine": {
                    "Data": {
                      "UseSfid": true
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Identity.json"),
                identitySettingsContents ?? """
                {
                  "Engine": {
                    "Identity": {
                      "Authentication": {
                        "Mode": "None"
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Tenancy.json"),
                tenancySettingsContents ?? """
                {
                  "Engine": {
                    "Tenancy": {
                      "Enabled": false
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Audit.json"),
                auditSettingsContents ?? """
                {
                  "Engine": {
                    "Audit": {
                      "Enabled": true
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Messaging.json"),
                messagingSettingsContents ?? """
                {
                  "Engine": {
                    "Messaging": {
                      "Outbox": {
                        "Enabled": true
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Observability.json"),
                observabilitySettingsContents ?? """
                {
                  "Engine": {
                    "Observability": {
                      "LogManifestSummary": true,
                      "LogModuleSummary": true,
                      "LogCapabilitySummary": true,
                      "Telemetry": {
                        "Provider": "OpenTelemetry",
                        "Protocol": "otlp/http",
                        "ExportLogs": true,
                        "ExportMetrics": true,
                        "ExportTraces": true
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddEngine.Localization.json"),
                localizationSettingsContents ?? """
                {
                  "Engine": {
                    "Localization": {
                      "DefaultCulture": "en",
                      "SupportedCultures": [
                        "en",
                        "th"
                      ],
                      "Resources": {
                        "th": {
                          "engine.docs.rest.title": "Acme.Store API ภาษาไทย"
                        }
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "Observability", "Development.json"),
                developmentObservabilitySettingsContents ?? """
                {
                  "Serilog": {
                    "Using": [
                      "Serilog.Sinks.Console"
                    ],
                    "MinimumLevel": {
                      "Default": "Information",
                      "Override": {
                        "Microsoft": "Warning",
                        "Microsoft.Hosting.Lifetime": "Information",
                        "System": "Warning"
                      }
                    },
                    "WriteTo": [
                      {
                        "Name": "Console"
                      }
                    ],
                    "Properties": {
                      "Application": "Acme.Store.Host"
                    }
                  }
                }
                """);
        }

        if (includeDocumentationSurfaceAssets)
        {
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddOpenApi.json"),
                openApiSettingsContents ?? """
                {
                  "OpenApi": {
                    "Title": "Acme.Store API"
                  }
                }
                """);
            File.WriteAllText(
                Path.Combine(configurationsPath, "AddReferenceDocs.json"),
                referenceDocsSettingsContents ?? """
                {
                  "ReferenceDocs": {
                    "Enabled": false,
                    "RoutePrefix": "/reference",
                    "DirectoryPath": "..\\..\\docs\\reference",
                    "DefaultDocument": "browse.html"
                  }
                }
                """);
        }

        if (includeLocalOrchestrationAssets)
        {
            File.WriteAllText(
                Path.Combine(appRootPath, "compose.yaml"),
                composeFileContents ?? """
                services:
                  acme-store:
                    build:
                      context: .
                      dockerfile: Dockerfile
                    environment:
                      ASPNETCORE_HTTP_PORTS: 8080
                      DOTNET_ENVIRONMENT: Container
                      Engine__Observability__Telemetry__Provider: OpenTelemetry
                      Engine__Observability__Telemetry__Protocol: otlp/http
                      Engine__Observability__Telemetry__Endpoint: http://otel-collector:4318
                    ports:
                      - "8080:8080"
                    depends_on:
                      - otel-collector

                  otel-collector:
                    image: otel/opentelemetry-collector-contrib:0.149.0
                    command:
                      - "--config=/etc/otelcol-contrib/config.yaml"
                    volumes:
                      - ./otel-collector-config.yaml:/etc/otelcol-contrib/config.yaml:ro
                    ports:
                      - "13133:13133"
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "otel-collector-config.yaml"),
                otelCollectorConfigContents ?? """
                extensions:
                  health_check:
                    endpoint: 0.0.0.0:13133

                receivers:
                  otlp:
                    protocols:
                      http:
                        endpoint: 0.0.0.0:4318

                processors:
                  batch: {}

                exporters:
                  debug:
                    verbosity: normal

                service:
                  extensions:
                    - health_check
                  pipelines:
                    logs:
                      receivers:
                        - otlp
                      processors:
                        - batch
                      exporters:
                        - debug
                    metrics:
                      receivers:
                        - otlp
                      processors:
                        - batch
                      exporters:
                        - debug
                    traces:
                      receivers:
                        - otlp
                      processors:
                        - batch
                      exporters:
                        - debug
                """);
        }

        if (includeDeploymentAssets)
        {
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "container-image"));
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "azure-container-apps"));
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "kubernetes"));

            var resolvedDockerSdkImageTag = dockerSdkImageTag ?? GetExpectedDockerImageTag(targetFramework);
            var resolvedDockerAspNetImageTag = dockerAspNetImageTag ?? GetExpectedDockerImageTag(targetFramework);

            File.WriteAllText(
                Path.Combine(appRootPath, "Dockerfile"),
                $$"""
                FROM mcr.microsoft.com/dotnet/sdk:{{resolvedDockerSdkImageTag}} AS build
                WORKDIR /src
                COPY . .
                RUN dotnet publish src/Acme.Store.Host/Acme.Store.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

                FROM mcr.microsoft.com/dotnet/aspnet:{{resolvedDockerAspNetImageTag}} AS final
                WORKDIR /app
                COPY --from=build /app/publish .
                ENTRYPOINT ["dotnet", "Acme.Store.Host.dll"]
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "container-image", "publish-image.ps1"),
                containerImagePublishScriptContents ?? """
                param(
                    [string]$Image = "replace-with-registry/acme-store:latest",
                    [string[]]$AdditionalTags = @(),
                    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
                    [string]$DockerfilePath = (Join-Path (Join-Path $PSScriptRoot "..\..") "Dockerfile"),
                    [switch]$Push,
                    [switch]$Preview
                )

                function Get-DockerBuildArguments {
                    param(
                        [string]$ResolvedSourceRoot,
                        [string]$ResolvedDockerfilePath,
                        [string[]]$ImageTags
                    )

                    $arguments = @("build", "-f", $ResolvedDockerfilePath)
                    foreach ($tag in $ImageTags) {
                        $arguments += @("-t", $tag)
                    }

                    $arguments += $ResolvedSourceRoot
                    return $arguments
                }

                $resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
                $resolvedDockerfilePath = (Resolve-Path -LiteralPath $DockerfilePath).Path
                $nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"
                $imageTags = @($Image) + $AdditionalTags
                $buildArguments = Get-DockerBuildArguments -ResolvedSourceRoot $resolvedSourceRoot -ResolvedDockerfilePath $resolvedDockerfilePath -ImageTags $imageTags

                if ($Preview) {
                    Write-Host (Format-Command -Command "docker" -Arguments $buildArguments)
                    Write-Host "Push skipped. Re-run with -Push when the target registry is ready."
                    return
                }

                if ($Push) {
                    foreach ($tag in $imageTags) {
                        & docker @("push", $tag)
                    }
                }

                Write-Host "Container image publishing completed successfully."
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "azure-container-apps", "deploy-up.ps1"),
                azureContainerAppsDeployScriptContents ?? """
                param(
                    [string]$ResourceGroupName = "replace-with-resource-group",
                    [string]$AppName = "acme-store",
                    [string]$Location = "replace-with-azure-region",
                    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
                    [string[]]$EnvironmentVariables = @(
                        "ASPNETCORE_HTTP_PORTS=8080",
                        "DOTNET_ENVIRONMENT=Production"),
                    [switch]$Preview
                )

                $resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
                $dockerfilePath = Join-Path $resolvedSourceRoot "Dockerfile"
                $nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"
                $hostProjectPath = Join-Path $resolvedSourceRoot "src\Acme.Store.Host\Acme.Store.Host.csproj"
                $upArguments = @("containerapp", "up", "--name", $AppName, "--resource-group", $ResourceGroupName, "--location", $Location, "--source", $resolvedSourceRoot, "--env-vars") + $EnvironmentVariables

                if ($Preview) {
                    Write-Host "Detected generated host project: $hostProjectPath"
                    Write-Host "Preview only."
                    Write-Host "az @upArguments"
                    return
                }

                & az @upArguments
                Write-Host "Azure Container Apps deployment completed successfully."
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "kubernetes", "apply.ps1"),
                kubernetesApplyScriptContents ?? """
                param(
                    [string]$Image = "replace-with-registry/acme-store:latest",
                    [string]$Namespace = "acme-store",
                    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
                    [string]$ManifestRoot = $PSScriptRoot,
                    [switch]$Preview
                )

                $resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
                $resolvedManifestRoot = (Resolve-Path -LiteralPath $ManifestRoot).Path
                $dockerfilePath = Join-Path $resolvedSourceRoot "Dockerfile"
                $nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"
                $kustomizationPath = Join-Path $resolvedManifestRoot "kustomization.yaml"
                $namespacePath = Join-Path $resolvedManifestRoot "namespace.yaml"
                $deploymentPath = Join-Path $resolvedManifestRoot "deployment.yaml"
                $servicePath = Join-Path $resolvedManifestRoot "service.yaml"
                $manifestPath = Join-Path $resolvedManifestRoot "rendered-manifest.yaml"

                if ($Preview) {
                    & kubectl @("kustomize", ".")
                    return
                }

                & kubectl @("apply", "-f", $manifestPath)
                Write-Host "Kubernetes deployment apply completed successfully."
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "kubernetes", "kustomization.yaml"),
                kubernetesKustomizationContents ?? """
                apiVersion: kustomize.config.k8s.io/v1beta1
                kind: Kustomization
                namespace: acme-store
                resources:
                - namespace.yaml
                - deployment.yaml
                - service.yaml
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "kubernetes", "namespace.yaml"),
                kubernetesNamespaceContents ?? """
                apiVersion: v1
                kind: Namespace
                metadata:
                  name: acme-store
                  labels:
                    app.kubernetes.io/name: acme-store
                    app.kubernetes.io/part-of: cephalon
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "kubernetes", "deployment.yaml"),
                kubernetesDeploymentContents ?? """
                apiVersion: apps/v1
                kind: Deployment
                metadata:
                  name: acme-store
                  labels:
                    app.kubernetes.io/name: acme-store
                    app.kubernetes.io/part-of: cephalon
                    app.kubernetes.io/component: host
                spec:
                  replicas: 1
                  selector:
                    matchLabels:
                      app.kubernetes.io/name: acme-store
                      app.kubernetes.io/component: host
                  template:
                    metadata:
                      labels:
                        app.kubernetes.io/name: acme-store
                        app.kubernetes.io/part-of: cephalon
                        app.kubernetes.io/component: host
                    spec:
                      containers:
                      - name: acme-store
                        image: replace-with-registry/acme-store:latest
                        imagePullPolicy: IfNotPresent
                        ports:
                        - containerPort: 8080
                          name: http
                        env:
                        - name: ASPNETCORE_HTTP_PORTS
                          value: "8080"
                        - name: DOTNET_ENVIRONMENT
                          value: Production
                        readinessProbe:
                          httpGet:
                            path: /health/ready
                            port: http
                          initialDelaySeconds: 5
                          periodSeconds: 10
                          timeoutSeconds: 5
                          failureThreshold: 3
                        livenessProbe:
                          httpGet:
                            path: /health/live
                            port: http
                          initialDelaySeconds: 15
                          periodSeconds: 20
                          timeoutSeconds: 5
                          failureThreshold: 3
                        startupProbe:
                          httpGet:
                            path: /health/ready
                            port: http
                          periodSeconds: 5
                          timeoutSeconds: 5
                          failureThreshold: 24
                        resources:
                          requests:
                            cpu: 100m
                            memory: 128Mi
                          limits:
                            cpu: 500m
                            memory: 512Mi
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "kubernetes", "service.yaml"),
                kubernetesServiceContents ?? """
                apiVersion: v1
                kind: Service
                metadata:
                  name: acme-store
                  labels:
                    app.kubernetes.io/name: acme-store
                    app.kubernetes.io/part-of: cephalon
                    app.kubernetes.io/component: host
                spec:
                  type: ClusterIP
                  selector:
                    app.kubernetes.io/name: acme-store
                    app.kubernetes.io/component: host
                  ports:
                  - name: http
                    port: 80
                    targetPort: http
                """);

            if (includeGeneratedGuidanceDocsAssets)
            {
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "container-image", "README.md"),
                    containerImageGuideContents ?? """
                    # Container image publishing

                    Use publish-image.ps1 with the generated Dockerfile.
                    Run docker login before you use -Push.
                    """);
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "azure-container-apps", "README.md"),
                    azureContainerAppsGuideContents ?? """
                    # Azure Container Apps deployment

                    Use deploy-up.ps1 from the generated app root.
                    The generated Dockerfile stays part of the az containerapp up --source flow.
                    """);
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "kubernetes", "README.md"),
                    kubernetesGuideContents ?? """
                    # Kubernetes deployment

                    Use apply.ps1 with kustomization.yaml, deployment.yaml, and service.yaml.
                    Preview the generated manifest with kubectl kustomize before apply.
                    """);
            }
        }

        if (includePublishedDeploymentAssets)
        {
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "windows-service"));
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "iis"));
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "azure-app-service"));
            Directory.CreateDirectory(Path.Combine(appRootPath, "deploy", "linux", "systemd"));

            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "windows-service", "install-service.ps1"),
                windowsServiceInstallScriptContents ?? """
                $serviceName = "Acme.Store"
                $publishedHostAssembly = "Acme.Store.Host.dll"
                sc.exe create $serviceName binPath= "dotnet $publishedHostAssembly"
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "windows-service", "remove-service.ps1"),
                windowsServiceRemoveScriptContents ?? """
                $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
                Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
                sc.exe delete $ServiceName
                Write-Host "Windows Service '$ServiceName' deleted successfully."
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "iis", "install-site.ps1"),
                iisInstallScriptContents ?? """
                $siteName = "Acme.Store"
                $webConfigPath = "web.config"
                Write-Output "$siteName $webConfigPath"
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "iis", "remove-site.ps1"),
                iisRemoveScriptContents ?? """
                $stopSiteArguments = @("stop", "site", "/site.name:$SiteName")
                $deleteSiteArguments = @("delete", "site", "/site.name:$SiteName")
                $deleteAppPoolArguments = @("delete", "apppool", "/apppool.name:$AppPoolName")
                Write-Host "IIS site '$SiteName' and app pool '$AppPoolName' deleted successfully."
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "azure-app-service", "deploy-zip.ps1"),
                azureAppServiceDeployScriptContents ?? """
                $appName = "Acme.Store"
                $packageName = "azure-app-service.zip"
                $entryAssembly = "Acme.Store.Host.dll"
                Write-Output "$appName $packageName $entryAssembly"
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "linux", "systemd", "Acme.Store.service"),
                linuxSystemdServiceContents ?? """
                [Unit]
                Description=Acme Store

                [Service]
                WorkingDirectory=/opt/Acme.Store/current
                EnvironmentFile=/etc/cephalon/Acme.Store.env
                ExecStart=/usr/bin/dotnet /opt/Acme.Store/current/Acme.Store.Host.dll

                [Install]
                WantedBy=multi-user.target
                """);
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "linux", "systemd", "Acme.Store.env"),
                linuxSystemdEnvironmentContents ?? """
                DOTNET_ENVIRONMENT=Production
                ASPNETCORE_URLS=http://0.0.0.0:8080
                # Engine__Observability__Telemetry__Endpoint=http://localhost:4318
                # Engine__Observability__Telemetry__ExportTraces=true
                """);

            if (includeGeneratedGuidanceDocsAssets)
            {
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "windows-service", "README.md"),
                    windowsServiceGuideContents ?? """
                    # Windows Service deployment

                    Publish through CephalonFolder.pubxml, then use install-service.ps1 and remove-service.ps1 for Acme.Store.
                    """);
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "iis", "README.md"),
                    iisGuideContents ?? """
                    # IIS deployment

                    Use install-site.ps1 and remove-site.ps1 for Acme.Store and keep web.config with the published output.
                    """);
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "azure-app-service", "README.md"),
                    azureAppServiceGuideContents ?? """
                    # Azure App Service deployment

                    Use deploy-zip.ps1 to push azure-app-service.zip for Acme.Store.
                    """);
                File.WriteAllText(
                    Path.Combine(appRootPath, "deploy", "linux", "systemd", "README.md"),
                    linuxSystemdGuideContents ?? """
                    # Linux systemd deployment

                    Install Acme.Store.service and Acme.Store.env, then manage the service with systemctl for Acme.Store.
                    """);
            }
        }

        if (includePublishProfile)
        {
            var publishProfilesPath = Path.Combine(appRootPath, "src", "Acme.Store.Host", "Properties", "PublishProfiles");
            Directory.CreateDirectory(publishProfilesPath);

            var publishProfileProperties = new List<string>();
            if (publishTrimmed)
            {
                publishProfileProperties.Add("    <PublishTrimmed>true</PublishTrimmed>");
            }

            if (publishAot)
            {
                publishProfileProperties.Add("    <PublishAot>true</PublishAot>");
            }

            if (publishSingleFile)
            {
                publishProfileProperties.Add("    <PublishSingleFile>true</PublishSingleFile>");
            }

            var publishProfileContents = publishProfileProperties.Count == 0
                ? "<Project />"
                : $$"""
                <Project>
                  <PropertyGroup>
                {{string.Join(Environment.NewLine, publishProfileProperties)}}
                  </PropertyGroup>
                </Project>
                """;

            File.WriteAllText(Path.Combine(publishProfilesPath, "CephalonFolder.pubxml"), publishProfileContents);
        }
    }

    private static void CreateTemplatePackDoctorAppRoot(string appRootPath, string appName)
    {
        var templateRoot = RepositoryPaths.GetDirectory(
            "templates",
            "Cephalon.TemplatePack",
            "templates",
            "cephalon-modular-monolith");
        var normalizedAppName = appName.Replace(" ", string.Empty, StringComparison.Ordinal);
        var appSlug = normalizedAppName.Replace('.', '-').ToLowerInvariant();

        CopyDirectory(templateRoot, appRootPath);

        var originalProjectPath = Path.Combine(appRootPath, "CephalonTemplateApp.csproj");
        var renamedProjectPath = Path.Combine(appRootPath, $"{normalizedAppName}.csproj");
        File.Move(originalProjectPath, renamedProjectPath);

        var systemdRoot = Path.Combine(appRootPath, "deploy", "linux", "systemd");
        File.Move(
            Path.Combine(systemdRoot, "CephalonTemplateApp.service"),
            Path.Combine(systemdRoot, $"{normalizedAppName}.service"));
        File.Move(
            Path.Combine(systemdRoot, "CephalonTemplateApp.env"),
            Path.Combine(systemdRoot, $"{normalizedAppName}.env"));

        foreach (var filePath in Directory.GetFiles(appRootPath, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(filePath).Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var contents = File.ReadAllText(filePath);
            contents = contents.Replace("CephalonTemplateApp", normalizedAppName, StringComparison.Ordinal);
            contents = contents.Replace("cephalon-template-app", appSlug, StringComparison.Ordinal);
            File.WriteAllText(filePath, contents);
        }

        var localPackageFeedPath = Path.Combine(appRootPath, ".cephalon", "packages");
        Directory.CreateDirectory(localPackageFeedPath);
        File.WriteAllText(
            Path.Combine(localPackageFeedPath, "Cephalon.AspNetCore.0.1.0-preview.nupkg"),
            "template-pack doctor fixture");
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var directoryPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativeDirectoryPath = Path.GetRelativePath(sourcePath, directoryPath);
            Directory.CreateDirectory(Path.Combine(destinationPath, relativeDirectoryPath));
        }

        foreach (var filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativeFilePath = Path.GetRelativePath(sourcePath, filePath);
            var destinationFilePath = Path.Combine(destinationPath, relativeFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
            File.Copy(filePath, destinationFilePath, overwrite: true);
        }
    }

    private static string GetExpectedDockerImageTag(string targetFramework)
    {
        var normalized = targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase)
            ? targetFramework[3..]
            : targetFramework;

        var tagCharacters = normalized
            .TakeWhile(character => char.IsDigit(character) || character == '.')
            .ToArray();

        return tagCharacters.Length == 0
            ? "10.0"
            : new string(tagCharacters);
    }

    private static string QuotePowerShellArgument(string value)
    {
        return value.Contains(' ', StringComparison.Ordinal)
            ? $"\"{value}\""
            : value;
    }

    private static string GetCurrentBuildConfiguration()
    {
        return AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
    }

    private static async Task<JsonObject> LoadJsonObjectAsync(string path)
    {
        var parsed = JsonNode.Parse(await File.ReadAllTextAsync(path));
        return Assert.IsType<JsonObject>(parsed);
    }

    private static string PackReferenceModulePackage(string outputPath)
    {
        var repositoryRoot = RepositoryPaths.GetRepositoryRoot();
        var projectPath = RepositoryPaths.GetFile(
            "samples",
            "Cephalon.ReferenceModule.Operations",
            "Cephalon.ReferenceModule.Operations.csproj");
        var result = RunProcess(
            "dotnet",
            $"pack \"{projectPath}\" -c {GetCurrentBuildConfiguration()} -o \"{outputPath}\" --no-build",
            repositoryRoot);

        Assert.True(
            result.ExitCode == 0,
            $"dotnet pack failed with exit code {result.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{result.Output}{Environment.NewLine}Error:{Environment.NewLine}{result.Error}");

        return Directory.GetFiles(outputPath, "Cephalon.ReferenceModule.Operations.*.nupkg", SearchOption.TopDirectoryOnly)
            .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)TimeSpan.FromMinutes(10).TotalMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout check and the kill request.
            }

            System.Threading.Tasks.Task.WaitAll(
                new System.Threading.Tasks.Task[] { outputTask, errorTask },
                TimeSpan.FromSeconds(5));

            var timeoutOutput = outputTask.IsCompletedSuccessfully ? outputTask.Result : string.Empty;
            var timeoutError = errorTask.IsCompletedSuccessfully ? errorTask.Result : string.Empty;
            timeoutError = string.IsNullOrWhiteSpace(timeoutError)
                ? "Process timed out after 10 minutes."
                : $"{timeoutError}{Environment.NewLine}Process timed out after 10 minutes.";

            return new ProcessResult(-1, timeoutOutput, timeoutError);
        }

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
