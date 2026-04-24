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
        bool includePublishProfile)
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
        File.WriteAllText(hostProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        File.WriteAllText(Path.Combine(appRootPath, "src", "Acme.Store.Host", "appsettings.json"), "{}");

        if (includePublishProfile)
        {
            var publishProfilesPath = Path.Combine(appRootPath, "src", "Acme.Store.Host", "Properties", "PublishProfiles");
            Directory.CreateDirectory(publishProfilesPath);
            File.WriteAllText(Path.Combine(publishProfilesPath, "CephalonFolder.pubxml"), "<Project />");
        }
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
