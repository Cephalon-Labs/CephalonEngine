using Cephalon.Cli;
using Cephalon.Cli.Commands;
using Cephalon.Tests.Support;
using System.Text.Json.Nodes;

namespace Cephalon.Tests.Tooling;

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

            var publishProfile = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "Properties", "PublishProfiles", "CephalonFolder.pubxml"));
            Assert.Contains("PublishDir", publishProfile, StringComparison.Ordinal);
            Assert.Contains("UseAppHost>false", publishProfile, StringComparison.Ordinal);
            Assert.Contains("../../artifacts/publish", publishProfile, StringComparison.Ordinal);

            var windowsInstallScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "windows-service", "install-service.ps1"));
            Assert.Contains("sc.exe create", windowsInstallScript, StringComparison.Ordinal);
            Assert.Contains("--contentRoot", windowsInstallScript, StringComparison.Ordinal);
            Assert.Contains("Acme.Store.Service.dll", windowsInstallScript, StringComparison.Ordinal);

            var iisInstallScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "iis", "install-site.ps1"));
            Assert.Contains("add apppool", iisInstallScript, StringComparison.Ordinal);
            Assert.Contains("add site", iisInstallScript, StringComparison.Ordinal);
            Assert.Contains("C:\\inetpub\\sites\\Acme.Store\\current", iisInstallScript, StringComparison.Ordinal);

            var azureAppServiceDeployScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "azure-app-service", "deploy-zip.ps1"));
            Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", azureAppServiceDeployScript, StringComparison.Ordinal);
            Assert.Contains("az @deployArguments", azureAppServiceDeployScript, StringComparison.Ordinal);
            Assert.Contains("azure-app-service.zip", azureAppServiceDeployScript, StringComparison.Ordinal);

            var containerImagePublishScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "container-image", "publish-image.ps1"));
            Assert.Contains("Get-DockerBuildArguments", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Format-Command -Command \"docker\"", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("@(\"push\", $tag)", containerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Container image publishing completed successfully.", containerImagePublishScript, StringComparison.Ordinal);

            var azureContainerAppsDeployScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "azure-container-apps", "deploy-up.ps1"));
            Assert.Contains("az @upArguments", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("--source", azureContainerAppsDeployScript, StringComparison.Ordinal);
            Assert.Contains("ASPNETCORE_HTTP_PORTS=8080", azureContainerAppsDeployScript, StringComparison.Ordinal);

            var kubernetesApplyScript = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "apply.ps1"));
            Assert.Contains("kubectl", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("kustomize", kubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("Kubernetes deployment apply completed successfully.", kubernetesApplyScript, StringComparison.Ordinal);

            var kubernetesDeployment = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "kubernetes", "deployment.yaml"));
            Assert.Contains("replace-with-registry/acme-store:latest", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("/health/ready", kubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("/health/live", kubernetesDeployment, StringComparison.Ordinal);

            var systemdService = await File.ReadAllTextAsync(Path.Combine(outputPath, "deploy", "linux", "systemd", "Acme.Store.service"));
            Assert.Contains("EnvironmentFile=-/etc/cephalon/Acme.Store.env", systemdService, StringComparison.Ordinal);
            Assert.Contains("ExecStart=/usr/bin/env dotnet /opt/Acme.Store/current/Acme.Store.Service.dll", systemdService, StringComparison.Ordinal);
            Assert.Contains("DynamicUser=true", systemdService, StringComparison.Ordinal);

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
            Assert.Contains("[ok] Generated package baseline: Cephalon.AspNetCore 0.1.0-preview, Cephalon.Data 0.1.0-preview", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Cephalon package source: ./.cephalon/packages", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Cephalon local package feed:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains(".cephalon/packages", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Deployment-mode shipping baseline: Stable shipping floor 'net10.0', readiness lane 'net11.0' (assessment-only).", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Trim support contract: not-claimed. Trimming is not part of the current Cephalon support contract.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated host target framework: ./src/Acme.Store.Host/Acme.Store.Host.csproj targets net10.0 and stays on the stable shipping floor.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated documentation surface assets: ./src/Acme.Store.Host/Configurations/AddOpenApi.json and ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenAPI baseline: ./src/Acme.Store.Host/Configurations/AddOpenApi.json keeps the generated REST docs surface explicit with Title='Acme.Store API'.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated hosted reference docs baseline: ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json keeps hosted reference docs explicit with Enabled=false, RoutePrefix=/reference, DirectoryPath=..\\..\\docs\\reference, and DefaultDocument=browse.html.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local orchestration assets: ./compose.yaml and ./otel-collector-config.yaml are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated compose baseline: ./compose.yaml keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenTelemetry collector baseline: ./otel-collector-config.yaml keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated deployment assets: ./Dockerfile plus container-image, Azure Container Apps, and Kubernetes deployment assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Dockerfile baseline: ./Dockerfile uses sdk:10.0 and aspnet:10.0 for the stable shipping floor.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated self-hosted and hosted deployment assets: ./deploy/windows-service, ./deploy/iis, ./deploy/azure-app-service, and ./deploy/linux/systemd assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service baseline: ./deploy/windows-service/install-service.ps1 keeps the generated Windows Service install flow aligned with Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated IIS baseline: ./deploy/iis/install-site.ps1 keeps the generated IIS site/app-pool defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Azure App Service baseline: ./deploy/azure-app-service/deploy-zip.ps1 keeps the generated ZIP package and published host defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Linux systemd baseline: ./deploy/linux/systemd/Acme.Store.service keeps the generated Linux systemd unit aligned with Acme.Store and Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
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
            Assert.Contains("[warn] Generated host target framework: ./src/Acme.Store.Host/Acme.Store.Host.csproj targets net11.0 and stays on the assessment-only readiness lane.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated documentation surface assets: ./src/Acme.Store.Host/Configurations/AddOpenApi.json and ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenAPI baseline: ./src/Acme.Store.Host/Configurations/AddOpenApi.json keeps the generated REST docs surface explicit with Title='Acme.Store API'.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated hosted reference docs baseline: ./src/Acme.Store.Host/Configurations/AddReferenceDocs.json keeps hosted reference docs explicit with Enabled=false, RoutePrefix=/reference, DirectoryPath=..\\..\\docs\\reference, and DefaultDocument=browse.html.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated local orchestration assets: ./compose.yaml and ./otel-collector-config.yaml are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated compose baseline: ./compose.yaml keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated OpenTelemetry collector baseline: ./otel-collector-config.yaml keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated deployment assets: ./Dockerfile plus container-image, Azure Container Apps, and Kubernetes deployment assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[warn] Generated Dockerfile baseline: ./Dockerfile uses sdk:11.0 and aspnet:11.0 for the assessment-only readiness lane.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated self-hosted and hosted deployment assets: ./deploy/windows-service, ./deploy/iis, ./deploy/azure-app-service, and ./deploy/linux/systemd assets are present.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Windows Service baseline: ./deploy/windows-service/install-service.ps1 keeps the generated Windows Service install flow aligned with Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated IIS baseline: ./deploy/iis/install-site.ps1 keeps the generated IIS site/app-pool defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Azure App Service baseline: ./deploy/azure-app-service/deploy-zip.ps1 keeps the generated ZIP package and published host defaults aligned with Acme.Store.", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("[ok] Generated Linux systemd baseline: ./deploy/linux/systemd/Acme.Store.service keeps the generated Linux systemd unit aligned with Acme.Store and Acme.Store.Host.dll.", stdout.ToString(), StringComparison.Ordinal);
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
            iisInstallScriptContents: """
                $siteName = "Legacy.Store"
                Write-Output $siteName
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
        Assert.Contains("cephalon package stage", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Doctor options:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Package stage options:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr.ToString());
    }

    private static void CreateGeneratedDoctorAppRoot(
        string appRootPath,
        bool includeLocalPackages,
        bool includePublishProfile,
        string targetFramework = "net10.0",
        bool publishTrimmed = false,
        bool publishAot = false,
        bool publishSingleFile = false,
        bool includeDocumentationSurfaceAssets = true,
        bool includeDeploymentAssets = true,
        bool includePublishedDeploymentAssets = true,
        bool includeLocalOrchestrationAssets = true,
        string? openApiSettingsContents = null,
        string? referenceDocsSettingsContents = null,
        string? composeFileContents = null,
        string? otelCollectorConfigContents = null,
        string? dockerSdkImageTag = null,
        string? dockerAspNetImageTag = null,
        string? windowsServiceInstallScriptContents = null,
        string? iisInstallScriptContents = null,
        string? azureAppServiceDeployScriptContents = null,
        string? linuxSystemdServiceContents = null)
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
                <PackageVersion Include="Cephalon.Data" Version="0.1.0-preview" />
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
        File.WriteAllText(Path.Combine(localPackageFeedPath, "README.md"), "# Placeholder");
        if (includeLocalPackages)
        {
            File.WriteAllBytes(Path.Combine(localPackageFeedPath, "Cephalon.AspNetCore.0.1.0-preview.nupkg"), []);
        }

        var hostProjectPath = Path.Combine(appRootPath, "src", "Acme.Store.Host", "Acme.Store.Host.csproj");
        File.WriteAllText(
            hostProjectPath,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{targetFramework}}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(appRootPath, "src", "Acme.Store.Host", "appsettings.json"), "{}");

        if (includeDocumentationSurfaceAssets)
        {
            var configurationsPath = Path.Combine(appRootPath, "src", "Acme.Store.Host", "Configurations");
            Directory.CreateDirectory(configurationsPath);
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
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "container-image", "publish-image.ps1"), "Write-Output 'publish-image'");
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "azure-container-apps", "deploy-up.ps1"), "Write-Output 'deploy-up'");
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "kubernetes", "apply.ps1"), "Write-Output 'apply'");
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "kubernetes", "kustomization.yaml"), "resources: []");
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "kubernetes", "namespace.yaml"), "apiVersion: v1");
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "kubernetes", "deployment.yaml"), "apiVersion: apps/v1");
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "kubernetes", "service.yaml"), "apiVersion: v1");
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
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "windows-service", "remove-service.ps1"), "sc.exe delete Acme.Store");
            File.WriteAllText(
                Path.Combine(appRootPath, "deploy", "iis", "install-site.ps1"),
                iisInstallScriptContents ?? """
                $siteName = "Acme.Store"
                $webConfigPath = "web.config"
                Write-Output "$siteName $webConfigPath"
                """);
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "iis", "remove-site.ps1"), "Write-Output 'remove-site'");
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
            File.WriteAllText(Path.Combine(appRootPath, "deploy", "linux", "systemd", "Acme.Store.env"), "ASPNETCORE_URLS=http://0.0.0.0:8080");
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

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
