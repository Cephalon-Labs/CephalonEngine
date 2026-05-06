using Cephalon.Cli.Console;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Implements the <c>cephalon doctor</c> environment verification command.
/// </summary>
internal static class DoctorCommand
{
    private const string DotNetExecutable = "dotnet";
    private const string RequiredTargetFramework = "net10.0";
    private const int RequiredMajorVersion = 10;
    private const string TemplatePackPackageId = "Cephalon.TemplatePack";
    private const string DotNetSdkDockerImagePrefix = "FROM mcr.microsoft.com/dotnet/sdk:";
    private const string DotNetAspNetDockerImagePrefix = "FROM mcr.microsoft.com/dotnet/aspnet:";
    private const string TemplatePackCustomHiveEnvironmentVariable = "CEPHALON_DOCTOR_TEMPLATE_HIVE";
    private const string RequiredScorecardSchemaVersion = "1.5.0";

    private static readonly string[] ExpectedTemplateShortNames =
    [
        "cephalon-monolith",
        "cephalon-slice",
        "cephalon-microservice",
        "cephalon-module",
        "cephalon-rest-module",
        "cephalon-rest-behavior-module"
    ];

    private static readonly string[] RequiredGeneratedContainerDeploymentAssetRelativePaths =
    [
        "Dockerfile",
        Path.Combine("deploy", "container-image", "publish-image.ps1"),
        Path.Combine("deploy", "azure-container-apps", "deploy-up.ps1"),
        Path.Combine("deploy", "kubernetes", "apply.ps1"),
        Path.Combine("deploy", "kubernetes", "kustomization.yaml"),
        Path.Combine("deploy", "kubernetes", "namespace.yaml"),
        Path.Combine("deploy", "kubernetes", "deployment.yaml"),
        Path.Combine("deploy", "kubernetes", "service.yaml")
    ];

    private static readonly string[] RequiredGeneratedLocalOrchestrationAssetRelativePaths =
    [
        "compose.yaml",
        "otel-collector-config.yaml"
    ];

    private static readonly string[] RequiredGeneratedPublishedDeploymentAssetRelativePaths =
    [
        Path.Combine("deploy", "windows-service", "install-service.ps1"),
        Path.Combine("deploy", "windows-service", "remove-service.ps1"),
        Path.Combine("deploy", "iis", "install-site.ps1"),
        Path.Combine("deploy", "iis", "remove-site.ps1"),
        Path.Combine("deploy", "azure-app-service", "deploy-zip.ps1")
    ];

    private static readonly string[] RequiredGeneratedSplitConfigurationAssetRelativePaths =
    [
        Path.Combine("Configurations", "AddEngine.AppModel.json"),
        Path.Combine("Configurations", "AddEngine.Data.json"),
        Path.Combine("Configurations", "AddEngine.Identity.json"),
        Path.Combine("Configurations", "AddEngine.Tenancy.json"),
        Path.Combine("Configurations", "AddEngine.Audit.json"),
        Path.Combine("Configurations", "AddEngine.Messaging.json"),
        Path.Combine("Configurations", "AddEngine.Observability.json"),
        Path.Combine("Configurations", "AddEngine.Localization.json"),
        Path.Combine("Configurations", "Observability", "Development.json")
    ];

    private static readonly (string Snippet, string Requirement)[] RequiredGeneratedHostBootstrapProgramMarkers =
    [
        ("builder.AddCephalonProjectConfigurations();", "AddCephalonProjectConfigurations"),
        ("builder.Host.UseWindowsService();", "UseWindowsService"),
        ("builder.Services.AddCephalonObservability(builder.Configuration);", "AddCephalonObservability"),
        ("builder.Configuration.GetSection(\"Serilog\").Exists()", "Serilog clear-provider guard"),
        ("builder.Logging.ClearProviders();", "ClearProviders"),
        ("builder.AddCephalonSerilog();", "AddCephalonSerilog"),
        ("builder.AddCephalonOpenTelemetry();", "AddCephalonOpenTelemetry"),
        ("app.MapCephalon();", "MapCephalon"),
        ("app.Run();", "Run")
    ];

    private static readonly (string Snippet, string Requirement)[] RequiredGeneratedHostBehaviorBootstrapProgramMarkers =
    [
        ("engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>", "AddBehaviors"),
        ("behaviors.AddHttpBehaviorBindings();", "AddHttpBehaviorBindings")
    ];

    private static readonly string[] RequiredGeneratedHostProjectPackageReferences =
    [
        "Cephalon.AspNetCore",
        "Cephalon.Engine.SourceGen",
        "Cephalon.Observability",
        "Cephalon.Observability.OpenTelemetry",
        "Cephalon.Observability.Serilog",
        "Microsoft.Extensions.Hosting.WindowsServices",
        "Serilog.Sinks.Console"
    ];

    private const string GeneratedConfigurationsContentPath = "Configurations/**/*.json";
    private const string GeneratedBehaviorSpecificationSearchPattern = "*BehaviorSpecifications.cs";
    private const string GeneratedCompositionSmokeTestMethodMarker = "Generated_scaffold_has_a_test_harness_ready_for_real_composition_checks";
    private const string GeneratedBehaviorSpecificationGivenMarker = "Given_";
    private const string GeneratedBehaviorSpecificationPlaceholderMarker = "then_replace_this_placeholder_with_the_first_failing_specification";
    private const string PreserveNewestValue = "PreserveNewest";

    private static readonly string[] RequiredGeneratedRootGuideMarkers =
    [
        "NuGet.config",
        ".cephalon/packages",
        "Configurations/Add*.json",
        "Configurations/Observability/Development.json",
        "CephalonFolder.pubxml",
        "deploy/windows-service/README.md",
        "deploy/iis/README.md",
        "deploy/azure-app-service/README.md",
        "deploy/container-image/README.md",
        "deploy/azure-container-apps/README.md",
        "deploy/kubernetes/README.md",
        "deploy/linux/systemd/README.md",
        "docker compose up --build",
        "/engine/snapshot"
    ];

    private static readonly string[] RequiredGeneratedConfigurationGuideMarkers =
    [
        "Configurations/Add*.json",
        "Configurations/{group}/{Environment}.json",
        "appsettings.json",
        "appsettings.{Environment}.json",
        "Configurations/Observability/Development.json",
        "AddCephalonProjectConfigurations()"
    ];

    private static readonly string[] RequiredGeneratedLocalPackageFeedGuideMarkers =
    [
        "Cephalon local package feed",
        "NuGet.config",
        "publish-package-artifacts.ps1",
        "replace the `cephalon` package source",
        "Dockerfile and compose path use the same restore configuration automatically."
    ];

    private static readonly string[] RequiredGeneratedContainerImageScriptMarkers =
    [
        "Dockerfile",
        "NuGet.config",
        "Get-DockerBuildArguments",
        "Format-Command -Command \"docker\"",
        "Container image publishing completed successfully.",
        "Push skipped. Re-run with -Push"
    ];

    private static readonly string[] RequiredGeneratedAzureContainerAppsScriptMarkers =
    [
        "Dockerfile",
        "NuGet.config",
        "az @upArguments",
        "--source",
        "ASPNETCORE_HTTP_PORTS=8080",
        "DOTNET_ENVIRONMENT=Production",
        "Azure Container Apps deployment completed successfully."
    ];

    private static readonly string[] RequiredGeneratedKubernetesApplyScriptMarkers =
    [
        "NuGet.config",
        "kustomization.yaml",
        "namespace.yaml",
        "deployment.yaml",
        "service.yaml",
        "rendered-manifest.yaml",
        "kubectl",
        "kustomize",
        "Kubernetes deployment apply completed successfully."
    ];

    private static readonly string[] RequiredGeneratedKubernetesKustomizationManifestMarkers =
    [
        "apiVersion: kustomize.config.k8s.io/v1beta1",
        "kind: Kustomization",
        "resources:",
        "- namespace.yaml",
        "- deployment.yaml",
        "- service.yaml"
    ];

    private static readonly string[] RequiredGeneratedKubernetesNamespaceManifestMarkers =
    [
        "apiVersion: v1",
        "kind: Namespace",
        "labels:",
        "app.kubernetes.io/part-of: cephalon"
    ];

    private static readonly string[] RequiredGeneratedKubernetesDeploymentManifestMarkers =
    [
        "apiVersion: apps/v1",
        "kind: Deployment",
        "app.kubernetes.io/part-of: cephalon",
        "app.kubernetes.io/component: host",
        "imagePullPolicy: IfNotPresent",
        "containerPort: 8080",
        "name: http",
        "- name: ASPNETCORE_HTTP_PORTS",
        "value: \"8080\"",
        "- name: DOTNET_ENVIRONMENT",
        "value: Production",
        "readinessProbe:",
        "/health/ready",
        "livenessProbe:",
        "/health/live",
        "startupProbe:",
        "resources:",
        "requests:",
        "limits:"
    ];

    private static readonly string[] RequiredGeneratedKubernetesServiceManifestMarkers =
    [
        "apiVersion: v1",
        "kind: Service",
        "app.kubernetes.io/part-of: cephalon",
        "app.kubernetes.io/component: host",
        "type: ClusterIP",
        "selector:",
        "- name: http",
        "port: 80",
        "targetPort: http"
    ];

    private static readonly string[] RequiredGeneratedWindowsServiceRemoveScriptMarkers =
    [
        "Get-Service -Name $ServiceName",
        "Stop-Service -Name $ServiceName",
        "sc.exe delete $ServiceName",
        "Windows Service '$ServiceName' deleted successfully."
    ];

    private static readonly string[] RequiredGeneratedIisRemoveScriptMarkers =
    [
        "$stopSiteArguments = @(\"stop\", \"site\", \"/site.name:$SiteName\")",
        "$deleteSiteArguments = @(\"delete\", \"site\", \"/site.name:$SiteName\")",
        "$deleteAppPoolArguments = @(\"delete\", \"apppool\", \"/apppool.name:$AppPoolName\")",
        "IIS site '$SiteName' and app pool '$AppPoolName' deleted successfully."
    ];

    private static readonly string[] RequiredGeneratedLinuxSystemdEnvironmentFileMarkers =
    [
        "DOTNET_ENVIRONMENT=Production",
        "ASPNETCORE_URLS=http://0.0.0.0:8080",
        "# Engine__Observability__Telemetry__Endpoint=http://localhost:4318",
        "# Engine__Observability__Telemetry__ExportTraces=true"
    ];

    /// <summary>
    /// Executes the doctor command with the supplied options.
    /// </summary>
    /// <param name="options">The parsed doctor options.</param>
    /// <param name="console">The console abstraction used for user-facing output.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunAsync(
        DoctorOptions options,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var evaluation = await EvaluateAsync(options, cancellationToken);

        await console.WriteOutputAsync("Cephalon doctor", cancellationToken);
        await console.WriteOutputAsync($"Cephalon scaffolds currently target '{RequiredTargetFramework}'.", cancellationToken);
        await console.WriteOutputAsync(string.Empty, cancellationToken);

        foreach (var check in evaluation.Checks)
        {
            await console.WriteOutputAsync(
                $"{GetSeverityLabel(check.Severity)} {check.Title}: {check.Detail}",
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(check.Guidance))
            {
                await console.WriteOutputAsync($"      {check.Guidance}", cancellationToken);
            }
        }

        await console.WriteOutputAsync(string.Empty, cancellationToken);

        if (evaluation.HasFailures)
        {
            await console.WriteErrorAsync(
                BuildDoctorFailureSummary(options, evaluation),
                cancellationToken);
            await console.WriteErrorAsync(
                BuildDoctorRequiredBaseline(options, evaluation),
                cancellationToken);
            return 1;
        }

        var templateInstallCommand = BuildTemplateInstallCommandText();
        var templateListCommand = BuildTemplateListCommandText();
        var templateStarterCommand = BuildTemplateStarterCommandText();
        var templateSummary = evaluation.TemplatePackInstalled
            ? "The optional template-pack path is also ready."
            : $"The optional template-pack path still needs `{templateInstallCommand}` if you want `dotnet new` starters in addition to `cephalon new`.";

        await console.WriteOutputAsync(
            evaluation.GeneratedApp is null
                ? $"Environment is ready for Cephalon CLI scaffolding. {templateSummary}"
                : $"Environment and generated app bootstrap are ready for Cephalon. {templateSummary}",
            cancellationToken);
        await console.WriteOutputAsync("Next steps:", cancellationToken);

        if (evaluation.GeneratedApp is null)
        {
            await console.WriteOutputAsync("  cephalon new Acme.Store --output ./Acme.Store", cancellationToken);
            await console.WriteOutputAsync("  cephalon doctor --app-root ./Acme.Store", cancellationToken);
            await console.WriteOutputAsync("  dotnet run --project ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj", cancellationToken);
        }
        else
        {
            await console.WriteOutputAsync(
                $"  Set-Location {FormatCommandPath(evaluation.GeneratedApp.ResolvedAppRootPath)}",
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(evaluation.GeneratedApp.SolutionRelativePath))
            {
                await console.WriteOutputAsync(
                    $"  dotnet restore {FormatCommandPath(evaluation.GeneratedApp.SolutionRelativePath)}",
                    cancellationToken);
            }
            else
            {
                await console.WriteOutputAsync(
                    $"  dotnet restore {FormatCommandPath(evaluation.GeneratedApp.HostProjectRelativePath!)}",
                    cancellationToken);
            }

            await console.WriteOutputAsync(
                $"  dotnet run --project {FormatCommandPath(evaluation.GeneratedApp.HostProjectRelativePath!)}",
                cancellationToken);
        }

        await console.WriteOutputAsync("  Browse /engine, /engine/manifest, /engine/snapshot, /health/ready, and /scalar once the host starts", cancellationToken);
        await console.WriteOutputAsync("Optional template-pack path:", cancellationToken);

        if (!evaluation.TemplatePackInstalled)
        {
            await console.WriteOutputAsync($"  {templateInstallCommand}", cancellationToken);
            await console.WriteOutputAsync($"  {templateListCommand}", cancellationToken);
        }

        await console.WriteOutputAsync($"  {templateStarterCommand}", cancellationToken);
        return 0;
    }

    private static string BuildDoctorFailureSummary(DoctorOptions options, DoctorEvaluation evaluation)
    {
        var hasScorecardPath = !string.IsNullOrWhiteSpace(options.ScorecardPath);

        if (evaluation.GeneratedApp is null && !hasScorecardPath)
        {
            return $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Install or select the missing prerequisites and rerun `cephalon doctor`.";
        }

        if (evaluation.GeneratedApp is not null && !hasScorecardPath)
        {
            return $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Fix the missing prerequisites or generated-app bootstrap blockers and rerun `cephalon doctor --app-root {FormatCommandPath(evaluation.GeneratedApp.ResolvedAppRootPath)}`.";
        }

        if (evaluation.GeneratedApp is null)
        {
            return $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Fix the missing prerequisites or scorecard artifact blockers and rerun `{BuildDoctorRerunCommand(options, evaluation)}`.";
        }

        return $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Fix the missing prerequisites, generated-app bootstrap blockers, or scorecard artifact blockers and rerun `{BuildDoctorRerunCommand(options, evaluation)}`.";
    }

    private static string BuildDoctorRequiredBaseline(DoctorOptions options, DoctorEvaluation evaluation)
    {
        var hasScorecardPath = !string.IsNullOrWhiteSpace(options.ScorecardPath);

        if (evaluation.GeneratedApp is null && !hasScorecardPath)
        {
            return "Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, and Microsoft.AspNetCore.App 10.x.";
        }

        if (evaluation.GeneratedApp is not null && !hasScorecardPath)
        {
            return "Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, Microsoft.AspNetCore.App 10.x, and a generated app root with a usable Cephalon package source plus host bootstrap assets.";
        }

        if (evaluation.GeneratedApp is null)
        {
            return $"Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, Microsoft.AspNetCore.App 10.x, and a readable schema {RequiredScorecardSchemaVersion} engine completion scorecard JSON artifact when `--scorecard` is supplied.";
        }

        return $"Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, Microsoft.AspNetCore.App 10.x, a generated app root with a usable Cephalon package source plus host bootstrap assets, and a readable schema {RequiredScorecardSchemaVersion} engine completion scorecard JSON artifact when `--scorecard` is supplied.";
    }

    private static string BuildDoctorRerunCommand(DoctorOptions options, DoctorEvaluation evaluation)
    {
        var command = new StringBuilder("cephalon doctor");

        if (!string.IsNullOrWhiteSpace(options.ScorecardPath))
        {
            command.Append(" --scorecard ");
            command.Append(FormatCommandPath(options.ScorecardPath!));
        }

        if (evaluation.GeneratedApp is not null)
        {
            command.Append(" --app-root ");
            command.Append(FormatCommandPath(evaluation.GeneratedApp.ResolvedAppRootPath));
        }
        else if (!string.IsNullOrWhiteSpace(options.AppRootPath))
        {
            command.Append(" --app-root ");
            command.Append(FormatCommandPath(options.AppRootPath!));
        }

        return command.ToString();
    }

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="DoctorOptions" /> instance.
    /// </summary>
    /// <param name="args">The raw arguments that follow the <c>doctor</c> command.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded.</param>
    /// <param name="error">When this method returns, contains the parse error if parsing failed.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    internal static bool TryParse(
        string[] args,
        out DoctorOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        string? appRootPath = null;
        string? scorecardPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--app-root":
                    if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                    {
                        error = "Option '--app-root' requires a value.";
                        return false;
                    }

                    appRootPath = args[index + 1].Trim();
                    index++;
                    break;
                case "--scorecard":
                    if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                    {
                        error = "Option '--scorecard' requires a value.";
                        return false;
                    }

                    scorecardPath = args[index + 1].Trim();
                    index++;
                    break;
                default:
                    error = $"Unknown option '{args[index]}'.";
                    return false;
            }
        }

        options = new DoctorOptions
        {
            AppRootPath = appRootPath,
            ScorecardPath = scorecardPath
        };
        return true;
    }

    private static async Task<DoctorEvaluation> EvaluateAsync(DoctorOptions options, CancellationToken cancellationToken)
    {
        var checks = new List<DoctorCheck>();

        var selectedSdkResult = await TryRunDotNetAsync(
            ["--version"],
            "dotnet SDK selection",
            "Install the .NET 10 SDK and ensure `dotnet --version` resolves to a 10.x SDK before building generated apps.",
            checks,
            cancellationToken);
        if (selectedSdkResult is null)
        {
            return new DoctorEvaluation(checks, false, null);
        }

        var currentSdkVersion = TryParseVersionToken(selectedSdkResult.Output);
        if (currentSdkVersion is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "dotnet SDK selection",
                $"Could not parse `dotnet --version` output: '{selectedSdkResult.Output.Trim()}'.",
                "Reinstall or repair the .NET SDK, then rerun `cephalon doctor`."));
        }
        else if (currentSdkVersion.Major < RequiredMajorVersion)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "dotnet SDK selection",
                $"Current SDK selection is '{currentSdkVersion}', but Cephalon scaffolds target '{RequiredTargetFramework}'.",
                "Update the active global.json or switch to a location where `dotnet --version` resolves to a 10.x SDK."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "dotnet SDK selection",
                currentSdkVersion.ToString(),
                null));
        }

        var sdkListResult = await TryRunDotNetAsync(
            ["--list-sdks"],
            "Installed net10.0 SDK family",
            "Install a .NET 10 SDK so generated Cephalon projects can restore and build.",
            checks,
            cancellationToken);
        if (sdkListResult is null)
        {
            return new DoctorEvaluation(checks, false, null);
        }

        var installedSdkVersions = ParseSdkVersions(sdkListResult.Output);
        var matchingSdkVersion = installedSdkVersions
            .Where(version => version.Major == RequiredMajorVersion)
            .OrderByDescending(version => version)
            .FirstOrDefault();

        if (matchingSdkVersion is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Installed net10.0 SDK family",
                "No 10.x SDK was found in `dotnet --list-sdks`.",
                "Install a .NET 10 SDK and rerun `cephalon doctor`."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Installed net10.0 SDK family",
                matchingSdkVersion.ToString(),
                null));
        }

        var runtimeListResult = await TryRunDotNetAsync(
            ["--list-runtimes"],
            "Required runtimes",
            "Install the .NET 10 SDK or the matching .NET 10 ASP.NET Core runtime bundle.",
            checks,
            cancellationToken);
        if (runtimeListResult is null)
        {
            return new DoctorEvaluation(checks, false, null);
        }

        AddRuntimeCheck(checks, runtimeListResult.Output, "Microsoft.NETCore.App");
        AddRuntimeCheck(checks, runtimeListResult.Output, "Microsoft.AspNetCore.App");

        var templatePackInstalled = false;
        var templateListCommand = BuildTemplateListCommandText();
        var templateInstallCommand = BuildTemplateInstallCommandText();
        var templateListResult = await TryRunDotNetAsync(
            BuildTemplateListArguments(),
            "Cephalon template pack",
            $"Install with `{templateInstallCommand}` if you want `dotnet new` starters.",
            checks,
            cancellationToken,
            treatFailureAsWarning: true,
            reportNonZeroExit: false);

        if (templateListResult is not null)
        {
            var discoveredTemplateShortNames = ExpectedTemplateShortNames
                .Where(shortName => templateListResult.Output.Contains(shortName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (discoveredTemplateShortNames.Length > 0)
            {
                templatePackInstalled = true;
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Cephalon template pack",
                    string.Join(", ", discoveredTemplateShortNames),
                    null));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Warning,
                    "Cephalon template pack",
                    $"No Cephalon templates were found by `{templateListCommand}`.",
                    $"Install with `{templateInstallCommand}` and verify with `{templateListCommand}`."));
            }
        }

        DeploymentModeSupportContract? supportContract = null;
        if (!DeploymentModeSupportContract.TryLoad(out supportContract, out var supportContractError) || supportContract is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Warning,
                "Deployment-mode support contract",
                $"Could not load the packaged deployment-mode support contract: {supportContractError}",
                "Reinstall Cephalon.Cli or inspect docs/deployment-mode-support.md from the matching repository snapshot."));
        }
        else
        {
            AddDeploymentModeSupportChecks(checks, supportContract);
        }

        EvaluateEngineCompletionScorecard(options.ScorecardPath, checks);

        var generatedApp = EvaluateGeneratedApp(options.AppRootPath, supportContract, checks);
        return new DoctorEvaluation(checks, templatePackInstalled, generatedApp);
    }

    private static void AddDeploymentModeSupportChecks(
        ICollection<DoctorCheck> checks,
        DeploymentModeSupportContract supportContract)
    {
        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Deployment-mode shipping baseline",
            $"Stable shipping floor '{supportContract.ShippingBaseline.StableTargetFramework}', readiness lane '{supportContract.ShippingBaseline.ReadinessLaneTargetFramework}' ({supportContract.ShippingBaseline.ReadinessLaneStatus}).",
            null));

        AddDeploymentModeCheck(checks, "Trim support contract", supportContract.DeploymentModes.Trim);
        AddDeploymentModeCheck(checks, "Native AOT support contract", supportContract.DeploymentModes.NativeAot);
        AddDeploymentModeCheck(checks, "Single-file support contract", supportContract.DeploymentModes.SingleFile);

        var scopedPackages = supportContract.DeploymentModeEligibility?.Packages ?? [];
        var packageClaims = scopedPackages
            .Where(package => package.SupportedModes is { Count: > 0 })
            .Select(package => $"{package.PackageName}: {string.Join(", ", package.SupportedModes)} ({package.ClaimAuditTier})")
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (packageClaims.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Package-scoped deployment-mode claims",
                string.Join("; ", packageClaims),
                "These package-scoped claims do not change the global trim, Native AOT, or single-file support contract."));
        }
    }

    private static void AddDeploymentModeCheck(
        ICollection<DoctorCheck> checks,
        string title,
        DeploymentModeSupportMode supportMode)
    {
        var severity = string.Equals(supportMode.Status, "claimed", StringComparison.OrdinalIgnoreCase)
            ? DoctorCheckSeverity.Pass
            : DoctorCheckSeverity.Warning;
        var guidance = severity == DoctorCheckSeverity.Pass
            ? null
            : "Treat this deployment mode as unsupported for external adoption until the support contract explicitly changes.";

        checks.Add(new DoctorCheck(
            severity,
            title,
            $"{supportMode.Status}. {supportMode.Summary}",
            guidance));
    }

    private static void EvaluateEngineCompletionScorecard(
        string? scorecardPath,
        ICollection<DoctorCheck> checks)
    {
        if (string.IsNullOrWhiteSpace(scorecardPath))
        {
            return;
        }

        string resolvedScorecardPath;

        try
        {
            resolvedScorecardPath = Path.GetFullPath(scorecardPath.Trim());
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"Could not resolve '{scorecardPath}': {exception.Message}",
                "Pass the JSON file written by `scripts/publish-engine-completion-scorecard.ps1`, then rerun `cephalon doctor --scorecard <path>`."));
            return;
        }

        if (!File.Exists(resolvedScorecardPath))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"File '{resolvedScorecardPath}' does not exist.",
                "Run `pwsh ./scripts/publish-engine-completion-scorecard.ps1` or `pwsh ./scripts/validate-release.ps1`, then rerun `cephalon doctor --scorecard <path>`."));
            return;
        }

        JsonNode? scorecard;

        try
        {
            scorecard = JsonNode.Parse(File.ReadAllText(resolvedScorecardPath));
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"Could not parse '{resolvedScorecardPath}' as JSON: {exception.Message}",
                "Regenerate the scorecard artifact from the matching repository snapshot."));
            return;
        }

        if (scorecard is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"File '{resolvedScorecardPath}' did not contain a JSON document.",
                "Regenerate the scorecard artifact from the matching repository snapshot."));
            return;
        }

        var errors = new List<string>();
        var schemaVersion = GetRequiredScorecardString(scorecard, "$schemaVersion", errors);
        var sourceDocument = GetRequiredScorecardString(scorecard, "SourceDocument", errors);
        var conformanceMatrix = GetRequiredScorecardString(scorecard, "ConformanceMatrix", errors);
        var summary = scorecard["Summary"];

        if (summary is null)
        {
            errors.Add("Summary");
        }

        if (errors.Count > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"Artifact '{resolvedScorecardPath}' is missing required scorecard fields: {string.Join(", ", errors)}.",
                "Regenerate the scorecard artifact with the current `scripts/publish-engine-completion-scorecard.ps1` before using it with doctor."));
            return;
        }

        if (!string.Equals(schemaVersion, RequiredScorecardSchemaVersion, StringComparison.Ordinal))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"Unsupported schema '{schemaVersion}'. Doctor expects scorecard schema '{RequiredScorecardSchemaVersion}'.",
                "Regenerate the scorecard artifact with the current `scripts/publish-engine-completion-scorecard.ps1`."));
            return;
        }

        var srePostureEvidence = scorecard["SrePostureEvidence"];
        var supplyChainEvidence = scorecard["SupplyChainEvidence"];
        var publicApiCompatibilityEvidence = scorecard["PublicApiCompatibilityEvidence"];

        if (srePostureEvidence is null)
        {
            errors.Add("SrePostureEvidence");
        }

        if (supplyChainEvidence is null)
        {
            errors.Add("SupplyChainEvidence");
        }

        if (publicApiCompatibilityEvidence is null)
        {
            errors.Add("PublicApiCompatibilityEvidence");
        }

        var platformGateCount = GetRequiredScorecardInt(summary, "PlatformGateCount", errors);
        var blockedPlatformGates = GetRequiredScorecardInt(summary, "BlockedPlatformGates", errors);
        var needsRefreshGates = GetRequiredScorecardInt(summary, "NeedsRefreshGates", errors);
        var partialPlatformGates = GetRequiredScorecardInt(summary, "PartialPlatformGates", errors);
        var notClaimedPlatformGates = GetRequiredScorecardInt(summary, "NotClaimedPlatformGates", errors);
        var evidenceSourceReferenceCount = GetRequiredScorecardInt(summary, "EvidenceSourceReferenceCount", errors);
        var packageGAReadinessCount = GetRequiredScorecardInt(summary, "PackageGAReadinessCount", errors);
        var partialPackageGAGates = GetRequiredScorecardInt(summary, "PartialPackageGAGates", errors);
        var notClaimedPackageGAGates = GetRequiredScorecardInt(summary, "NotClaimedPackageGAGates", errors);
        var needsRefreshPackageGAGates = GetRequiredScorecardInt(summary, "NeedsRefreshPackageGAGates", errors);
        var sreSliCount = GetRequiredScorecardInt(summary, "SreSliCount", errors);
        var sreTargetDeclaredCount = GetRequiredScorecardInt(summary, "SreTargetDeclaredCount", errors);
        var srePendingStableBaselineCount = GetRequiredScorecardInt(summary, "SrePendingStableBaselineCount", errors);
        var sreStableBaselineCount = GetRequiredScorecardInt(summary, "SreStableBaselineCount", errors);
        var supplyChainEvidenceItemCount = GetRequiredScorecardInt(summary, "SupplyChainEvidenceItemCount", errors);
        var supplyChainWorkflowReadyCount = GetRequiredScorecardInt(summary, "SupplyChainWorkflowReadyCount", errors);
        var supplyChainExternalPolicyPendingCount = GetRequiredScorecardInt(summary, "SupplyChainExternalPolicyPendingCount", errors);
        var supplyChainBlockedCount = GetRequiredScorecardInt(summary, "SupplyChainBlockedCount", errors);
        var publicApiPackageCount = GetRequiredScorecardInt(summary, "PublicApiPackageCount", errors);
        var publicApiPendingPackageCount = GetRequiredScorecardInt(summary, "PublicApiPendingPackageCount", errors);
        var publicApiAdditiveEntryCount = GetRequiredScorecardInt(summary, "PublicApiAdditiveEntryCount", errors);
        var publicApiRemovalEntryCount = GetRequiredScorecardInt(summary, "PublicApiRemovalEntryCount", errors);
        var evidenceSreSliCount = GetRequiredScorecardInt(srePostureEvidence, "SliCount", errors, "SrePostureEvidence");
        var evidenceSreTargetDeclaredCount = GetRequiredScorecardInt(srePostureEvidence, "TargetDeclaredCount", errors, "SrePostureEvidence");
        var evidenceSrePendingStableBaselineCount = GetRequiredScorecardInt(srePostureEvidence, "PendingStableBaselineCount", errors, "SrePostureEvidence");
        var evidenceSreStableBaselineCount = GetRequiredScorecardInt(srePostureEvidence, "StableBaselineCount", errors, "SrePostureEvidence");
        var evidenceSupplyChainEvidenceItemCount = GetRequiredScorecardInt(supplyChainEvidence, "EvidenceItemCount", errors, "SupplyChainEvidence");
        var evidenceSupplyChainWorkflowReadyCount = GetRequiredScorecardInt(supplyChainEvidence, "WorkflowReadyCount", errors, "SupplyChainEvidence");
        var evidenceSupplyChainExternalPolicyPendingCount = GetRequiredScorecardInt(supplyChainEvidence, "ExternalPolicyPendingCount", errors, "SupplyChainEvidence");
        var evidenceSupplyChainBlockedCount = GetRequiredScorecardInt(supplyChainEvidence, "BlockedCount", errors, "SupplyChainEvidence");
        var evidencePublicApiPackageCount = GetRequiredScorecardInt(publicApiCompatibilityEvidence, "PackageCount", errors, "PublicApiCompatibilityEvidence");
        var evidencePublicApiPendingPackageCount = GetRequiredScorecardInt(publicApiCompatibilityEvidence, "PendingPackageCount", errors, "PublicApiCompatibilityEvidence");
        var evidencePublicApiAdditiveEntryCount = GetRequiredScorecardInt(publicApiCompatibilityEvidence, "AdditiveEntryCount", errors, "PublicApiCompatibilityEvidence");
        var evidencePublicApiRemovalEntryCount = GetRequiredScorecardInt(publicApiCompatibilityEvidence, "RemovalEntryCount", errors, "PublicApiCompatibilityEvidence");

        if (errors.Count > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard artifact",
                $"Artifact '{resolvedScorecardPath}' is missing required scorecard fields: {string.Join(", ", errors)}.",
                "Regenerate the scorecard artifact with the current `scripts/publish-engine-completion-scorecard.ps1` before using it with doctor."));
            return;
        }

        if (sreSliCount != evidenceSreSliCount ||
            sreTargetDeclaredCount != evidenceSreTargetDeclaredCount ||
            srePendingStableBaselineCount != evidenceSrePendingStableBaselineCount ||
            sreStableBaselineCount != evidenceSreStableBaselineCount)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard SRE posture",
                $"Artifact '{resolvedScorecardPath}' has SRE summary counts that do not match SrePostureEvidence.",
                "Regenerate the scorecard artifact with the current `scripts/publish-engine-completion-scorecard.ps1`."));
            return;
        }

        if (supplyChainEvidenceItemCount != evidenceSupplyChainEvidenceItemCount ||
            supplyChainWorkflowReadyCount != evidenceSupplyChainWorkflowReadyCount ||
            supplyChainExternalPolicyPendingCount != evidenceSupplyChainExternalPolicyPendingCount ||
            supplyChainBlockedCount != evidenceSupplyChainBlockedCount)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard supply-chain release evidence",
                $"Artifact '{resolvedScorecardPath}' has supply-chain summary counts that do not match SupplyChainEvidence.",
                "Regenerate the scorecard artifact with the current `scripts/publish-engine-completion-scorecard.ps1`."));
            return;
        }

        if (publicApiPackageCount != evidencePublicApiPackageCount ||
            publicApiPendingPackageCount != evidencePublicApiPendingPackageCount ||
            publicApiAdditiveEntryCount != evidencePublicApiAdditiveEntryCount ||
            publicApiRemovalEntryCount != evidencePublicApiRemovalEntryCount)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Engine completion scorecard public API compatibility",
                $"Artifact '{resolvedScorecardPath}' has public API summary counts that do not match PublicApiCompatibilityEvidence.",
                "Regenerate the scorecard artifact with the current `scripts/publish-engine-completion-scorecard.ps1`."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Engine completion scorecard artifact",
            $"schema {schemaVersion} from {sourceDocument}; conformance matrix {conformanceMatrix}.",
            null));

        var platformSeverity =
            blockedPlatformGates > 0 ||
            needsRefreshGates > 0 ||
            partialPlatformGates > 0 ||
            notClaimedPlatformGates > 0
                ? DoctorCheckSeverity.Warning
                : DoctorCheckSeverity.Pass;
        checks.Add(new DoctorCheck(
            platformSeverity,
            "Engine completion scorecard platform gates",
            $"{platformGateCount} gates; blocked {blockedPlatformGates}, needs-refresh {needsRefreshGates}, partial {partialPlatformGates}, not-claimed {notClaimedPlatformGates}.",
            platformSeverity == DoctorCheckSeverity.Pass
                ? null
                : "Treat this as release-readiness posture, not a local machine failure; update the owning source docs before promoting the scorecard."));

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Engine completion scorecard evidence references",
            $"{evidenceSourceReferenceCount} repo-local references validated by the published artifact.",
            null));

        var packageSeverity =
            partialPackageGAGates > 0 ||
            notClaimedPackageGAGates > 0 ||
            needsRefreshPackageGAGates > 0
                ? DoctorCheckSeverity.Warning
                : DoctorCheckSeverity.Pass;
        checks.Add(new DoctorCheck(
            packageSeverity,
            "Engine completion scorecard package GA readiness",
            $"{packageGAReadinessCount} package rows; partial {partialPackageGAGates}, not-claimed {notClaimedPackageGAGates}, needs-refresh {needsRefreshPackageGAGates}.",
            packageSeverity == DoctorCheckSeverity.Pass
                ? null
                : "Do not equate package maturity with GA; use this summary to find the source document and blocker class before release promotion."));

        var sreSeverity = srePendingStableBaselineCount > 0
            ? DoctorCheckSeverity.Warning
            : DoctorCheckSeverity.Pass;
        checks.Add(new DoctorCheck(
            sreSeverity,
            "Engine completion scorecard SRE posture",
            $"{sreSliCount} SLIs; target-declared {sreTargetDeclaredCount}, pending stable baselines {srePendingStableBaselineCount}, stable baselines {sreStableBaselineCount}.",
            sreSeverity == DoctorCheckSeverity.Pass
                ? null
                : "Treat SRE posture as release-readiness evidence, not a stable SLO claim, until baselineStatus entries move out of pending-stable-baseline."));

        var supplyChainSeverity = supplyChainBlockedCount > 0 ||
            supplyChainExternalPolicyPendingCount > 0
                ? DoctorCheckSeverity.Warning
                : DoctorCheckSeverity.Pass;
        checks.Add(new DoctorCheck(
            supplyChainSeverity,
            "Engine completion scorecard supply-chain release evidence",
            $"{supplyChainEvidenceItemCount} items; workflow-ready {supplyChainWorkflowReadyCount}, external-policy-pending {supplyChainExternalPolicyPendingCount}, blocked {supplyChainBlockedCount}.",
            supplyChainSeverity == DoctorCheckSeverity.Pass
                ? null
                : "Treat workflow-ready evidence as release-readiness posture only; complete nuget.org-side trusted publishing, prefix reservation, and repository secret policy before a real tag push."));

        var publicApiSeverity = publicApiRemovalEntryCount > 0
            ? DoctorCheckSeverity.Warning
            : DoctorCheckSeverity.Pass;
        checks.Add(new DoctorCheck(
            publicApiSeverity,
            "Engine completion scorecard public API compatibility",
            $"{publicApiPackageCount} package baselines; pending packages {publicApiPendingPackageCount}, additions {publicApiAdditiveEntryCount}, removals {publicApiRemovalEntryCount}.",
            publicApiSeverity == DoctorCheckSeverity.Pass
                ? null
                : "Review public API removal entries and compatibility guidance before release promotion."));
    }

    private static string? GetRequiredScorecardString(
        JsonNode scorecard,
        string propertyName,
        List<string> errors)
    {
        try
        {
            var value = scorecard[propertyName]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(propertyName);
            }

            return value;
        }
        catch
        {
            errors.Add(propertyName);
            return null;
        }
    }

    private static int GetRequiredScorecardInt(
        JsonNode? scorecard,
        string propertyName,
        List<string> errors,
        string ownerName = "Summary")
    {
        if (scorecard is null)
        {
            return 0;
        }

        try
        {
            var value = scorecard[propertyName]?.GetValue<int>();

            if (value is null)
            {
                errors.Add($"{ownerName}.{propertyName}");
                return 0;
            }

            return value.Value;
        }
        catch
        {
            errors.Add($"{ownerName}.{propertyName}");
            return 0;
        }
    }

    private static GeneratedAppDoctorEvaluation? EvaluateGeneratedApp(
        string? requestedAppRootPath,
        DeploymentModeSupportContract? supportContract,
        ICollection<DoctorCheck> checks)
    {
        if (string.IsNullOrWhiteSpace(requestedAppRootPath))
        {
            return null;
        }

        string resolvedAppRootPath;

        try
        {
            resolvedAppRootPath = Path.GetFullPath(requestedAppRootPath.Trim());
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated app root",
                $"Could not resolve '{requestedAppRootPath}': {exception.Message}",
                "Pass a valid generated-app root path to `cephalon doctor --app-root`."));
            return null;
        }

        if (!Directory.Exists(resolvedAppRootPath))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated app root",
                $"Directory '{resolvedAppRootPath}' does not exist.",
                "Run `cephalon new <AppName>` first or point `--app-root` at an existing generated app root."));
            return new GeneratedAppDoctorEvaluation(resolvedAppRootPath, null, null);
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated app root",
            resolvedAppRootPath,
            null));

        var solutionPath = Directory.GetFiles(resolvedAppRootPath, "*.slnx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        var (hostProjects, layoutKind) = DiscoverGeneratedHostProjects(resolvedAppRootPath);

        if (hostProjects.Length == 0)
        {
            if (solutionPath is null)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated app solution",
                    "No `.slnx` file was found at the generated app root.",
                    "Use the root produced by `cephalon new`, a root emitted by `dotnet new cephalon-*`, or restore the solution file before rerunning doctor."));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Generated app solution",
                    ToDisplayRelativePath(resolvedAppRootPath, solutionPath),
                    null));
            }

            EvaluateGeneratedPackageBaseline(resolvedAppRootPath, null, GeneratedAppLayoutKind.Unknown, checks);

            var generatedNuGetConfigPath = Path.Combine(resolvedAppRootPath, "NuGet.config");
            if (!File.Exists(generatedNuGetConfigPath))
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Cephalon package source",
                    "Missing `NuGet.config` at the generated app root.",
                    "Restore the generated package-source bootstrap or regenerate the app before continuing."));
            }
            else
            {
                EvaluatePackageSource(generatedNuGetConfigPath, resolvedAppRootPath, checks);
            }

            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host project",
                "No generated host project with `appsettings.json` was found under `src/` or at the generated app root.",
                "Use the root emitted by `cephalon new` or `dotnet new cephalon-*`, or restore the generated host project before rerunning doctor."));
            return new GeneratedAppDoctorEvaluation(resolvedAppRootPath, solutionPath, null);
        }

        if (solutionPath is null)
        {
            if (layoutKind == GeneratedAppLayoutKind.ProjectRootStarter)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Generated app solution",
                    $"Template-pack project-root layout via {ToDisplayRelativePath(resolvedAppRootPath, hostProjects[0].ProjectPath)}.",
                    null));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated app solution",
                    "No `.slnx` file was found at the generated app root.",
                    "Use the root produced by `cephalon new`, a root emitted by `dotnet new cephalon-*`, or restore the solution file before rerunning doctor."));
            }
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated app solution",
                ToDisplayRelativePath(resolvedAppRootPath, solutionPath),
                null));
        }

        EvaluateGeneratedPackageBaseline(resolvedAppRootPath, hostProjects[0], layoutKind, checks);

        var nuGetConfigPath = Path.Combine(resolvedAppRootPath, "NuGet.config");
        if (!File.Exists(nuGetConfigPath))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Cephalon package source",
                "Missing `NuGet.config` at the generated app root.",
                "Restore the generated package-source bootstrap or regenerate the app before continuing."));
        }
        else
        {
            EvaluatePackageSource(nuGetConfigPath, resolvedAppRootPath, checks);
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated host project",
            string.Join(", ", hostProjects.Select(project => ToDisplayRelativePath(resolvedAppRootPath, project.ProjectPath))),
            null));

        var testProjectDirectories = Directory.Exists(Path.Combine(resolvedAppRootPath, "tests"))
            ? Directory.GetDirectories(Path.Combine(resolvedAppRootPath, "tests"), "*", SearchOption.TopDirectoryOnly)
            : [];

        var testProjects = testProjectDirectories
            .Select(directory => new
            {
                Directory = directory,
                ProjectPath = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault()
            })
            .Where(candidate => candidate.ProjectPath is not null)
            .Select(candidate => new GeneratedTestProject(
                candidate.Directory,
                candidate.ProjectPath!,
                Path.Combine(candidate.Directory, "Architecture", "CompositionSmokeTests.cs"),
                Path.Combine(candidate.Directory, "Features")))
            .OrderBy(candidate => candidate.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (testProjects.Length == 0)
        {
            if (layoutKind == GeneratedAppLayoutKind.ProjectRootStarter)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Generated test project",
                    "Template-pack project-root starters do not emit a default `tests/` project.",
                    null));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated test project",
                    "No generated test project was found under `tests/`.",
                    "Restore the scaffolded test project or regenerate the app before teams rely on the generated composition and behavior-specification harness."));
            }
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated test project",
                string.Join(", ", testProjects.Select(project => ToDisplayRelativePath(resolvedAppRootPath, project.ProjectPath))),
                null));
        }

        var selectedHostProject = hostProjects[0];
        EvaluateGeneratedAppSupportContract(selectedHostProject, resolvedAppRootPath, supportContract, checks);
        EvaluateGeneratedHostBootstrapBaseline(selectedHostProject, resolvedAppRootPath, checks);
        if (testProjects.Length > 0)
        {
            EvaluateGeneratedTestHarnessBaseline(testProjects, resolvedAppRootPath, checks);
        }
        EvaluateGeneratedSplitConfigurationAssets(selectedHostProject, resolvedAppRootPath, checks);
        EvaluateGeneratedDocumentationSurfaceAssets(selectedHostProject, resolvedAppRootPath, checks);
        EvaluateGeneratedAppDeploymentAssets(selectedHostProject, resolvedAppRootPath, solutionPath, supportContract, checks);
        EvaluateGeneratedGuidanceDocsBaseline(selectedHostProject, resolvedAppRootPath, solutionPath, checks);

        var missingPublishProfileProjects = hostProjects
            .Where(project => !File.Exists(project.PublishProfilePath))
            .Select(project => ToDisplayRelativePath(resolvedAppRootPath, project.ProjectPath))
            .ToArray();

        if (missingPublishProfileProjects.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated publish profile",
                $"Missing `CephalonFolder.pubxml` for: {string.Join(", ", missingPublishProfileProjects)}.",
                "Restore the generated publish profile or regenerate the app before replaying the published-output path."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated publish profile",
                string.Join(", ", hostProjects.Select(project => ToDisplayRelativePath(resolvedAppRootPath, project.PublishProfilePath))),
                null));
        }

        return new GeneratedAppDoctorEvaluation(resolvedAppRootPath, solutionPath, selectedHostProject.ProjectPath);
    }

    private static (GeneratedHostProject[] HostProjects, GeneratedAppLayoutKind LayoutKind) DiscoverGeneratedHostProjects(string generatedAppRootPath)
    {
        var hostProjectDirectories = Directory.Exists(Path.Combine(generatedAppRootPath, "src"))
            ? Directory.GetDirectories(Path.Combine(generatedAppRootPath, "src"), "*", SearchOption.TopDirectoryOnly)
            : [];

        var srcHostProjects = hostProjectDirectories
            .Select(directory => new
            {
                Directory = directory,
                ProjectPath = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault()
            })
            .Where(candidate => candidate.ProjectPath is not null)
            .Select(candidate => new GeneratedHostProject(
                candidate.Directory,
                candidate.ProjectPath!,
                Path.Combine(candidate.Directory, "Program.cs"),
                Path.Combine(candidate.Directory, "appsettings.json"),
                Path.Combine(candidate.Directory, "Properties", "PublishProfiles", "CephalonFolder.pubxml")))
            .Where(candidate => File.Exists(candidate.AppSettingsPath))
            .OrderBy(candidate => candidate.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (srcHostProjects.Length > 0)
        {
            return (srcHostProjects, GeneratedAppLayoutKind.SolutionWithSrcHost);
        }

        var rootProjectPath = Directory.GetFiles(generatedAppRootPath, "*.csproj", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (rootProjectPath is null)
        {
            return ([], GeneratedAppLayoutKind.Unknown);
        }

        var rootHostProject = new GeneratedHostProject(
            generatedAppRootPath,
            rootProjectPath,
            Path.Combine(generatedAppRootPath, "Program.cs"),
            Path.Combine(generatedAppRootPath, "appsettings.json"),
            Path.Combine(generatedAppRootPath, "Properties", "PublishProfiles", "CephalonFolder.pubxml"));

        return File.Exists(rootHostProject.AppSettingsPath)
            ? ([rootHostProject], GeneratedAppLayoutKind.ProjectRootStarter)
            : ([], GeneratedAppLayoutKind.Unknown);
    }

    private static void EvaluateGeneratedPackageBaseline(
        string generatedAppRootPath,
        GeneratedHostProject? hostProject,
        GeneratedAppLayoutKind layoutKind,
        ICollection<DoctorCheck> checks)
    {
        var packagePropsPath = Path.Combine(generatedAppRootPath, "Directory.Packages.props");
        if (File.Exists(packagePropsPath))
        {
            var cephalonPackageVersions = ExtractCephalonPackageVersions(packagePropsPath);
            if (cephalonPackageVersions.Length == 0)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated package baseline",
                    "`Directory.Packages.props` does not define any `Cephalon*` package versions.",
                    "Keep the generated package baseline intact or restore the Cephalon package references before rerunning doctor."));
                return;
            }

            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated package baseline",
                string.Join(", ", cephalonPackageVersions),
                null));
            return;
        }

        if (layoutKind == GeneratedAppLayoutKind.ProjectRootStarter && hostProject is not null)
        {
            var directPackageVersions = ExtractCephalonPackageReferenceVersions(hostProject.ProjectPath);
            if (directPackageVersions.Length == 0)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated package baseline",
                    $"{ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)} does not define any direct `Cephalon*` `PackageReference` versions.",
                    "Keep the generated template-pack package references intact or restore the app starter before rerunning doctor."));
                return;
            }

            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated package baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)} keeps direct Cephalon package versions explicit: {string.Join(", ", directPackageVersions)}",
                null));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Failure,
            "Generated package baseline",
            "Missing `Directory.Packages.props` at the generated app root.",
            "Restore the generated package baseline or regenerate the app before continuing."));
    }

    private static void EvaluateGeneratedAppSupportContract(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        DeploymentModeSupportContract? supportContract,
        ICollection<DoctorCheck> checks)
    {
        if (supportContract is null)
        {
            return;
        }

        XDocument projectDocument;
        try
        {
            projectDocument = XDocument.Load(hostProject.ProjectPath);
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host target framework",
                $"Could not inspect {ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)}: {exception.Message}",
                "Fix the generated host project file before rerunning `cephalon doctor --app-root`."));
            return;
        }

        var targetFrameworks = ExtractTargetFrameworks(projectDocument);
        var stableTargetFramework = supportContract.ShippingBaseline.StableTargetFramework;
        var readinessLaneTargetFramework = supportContract.ShippingBaseline.ReadinessLaneTargetFramework;
        var hostProjectDisplayPath = ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath);

        if (targetFrameworks.Length == 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host target framework",
                $"{hostProjectDisplayPath} does not declare `<TargetFramework>` or `<TargetFrameworks>`.",
                $"Retarget the generated host project to `{stableTargetFramework}` for the shipped baseline or `{readinessLaneTargetFramework}` for the assessment-only readiness lane."));
        }
        else
        {
            var unsupportedTargetFrameworks = targetFrameworks
                .Where(targetFramework =>
                    !string.Equals(targetFramework, stableTargetFramework, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetFramework, readinessLaneTargetFramework, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (unsupportedTargetFrameworks.Length > 0)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated host target framework",
                    $"{hostProjectDisplayPath} targets {string.Join(", ", targetFrameworks)}, which falls outside the current Cephalon support contract.",
                    $"Use `{stableTargetFramework}` for the supported shipping baseline or keep `{readinessLaneTargetFramework}` for assessment-only readiness work."));
            }
            else if (targetFrameworks.Any(targetFramework => string.Equals(targetFramework, readinessLaneTargetFramework, StringComparison.OrdinalIgnoreCase)))
            {
                var detail = targetFrameworks.All(targetFramework => string.Equals(targetFramework, readinessLaneTargetFramework, StringComparison.OrdinalIgnoreCase))
                    ? $"{hostProjectDisplayPath} targets {string.Join(", ", targetFrameworks)} and stays on the assessment-only readiness lane."
                    : $"{hostProjectDisplayPath} targets {string.Join(", ", targetFrameworks)} and includes the assessment-only readiness lane.";

                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Warning,
                    "Generated host target framework",
                    detail,
                    $"Keep `{stableTargetFramework}` for supported external adoption, or treat `{readinessLaneTargetFramework}` as readiness-only until the support contract changes."));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Generated host target framework",
                    $"{hostProjectDisplayPath} targets {string.Join(", ", targetFrameworks)} and stays on the stable shipping floor.",
                    null));
            }
        }

        XDocument? publishProfileDocument = null;
        if (File.Exists(hostProject.PublishProfilePath))
        {
            try
            {
                publishProfileDocument = XDocument.Load(hostProject.PublishProfilePath);
            }
            catch (Exception exception)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated publish profile contract",
                    $"Could not inspect {ToDisplayRelativePath(generatedAppRootPath, hostProject.PublishProfilePath)}: {exception.Message}",
                    "Fix the generated publish profile before rerunning `cephalon doctor --app-root`."));
            }
        }

        AddGeneratedAppDeploymentModeCheck(
            checks,
            generatedAppRootPath,
            "Generated app trim posture",
            "PublishTrimmed",
            supportContract.DeploymentModes.Trim,
            projectDocument,
            hostProject.ProjectPath,
            publishProfileDocument,
            hostProject.PublishProfilePath);

        AddGeneratedAppDeploymentModeCheck(
            checks,
            generatedAppRootPath,
            "Generated app Native AOT posture",
            "PublishAot",
            supportContract.DeploymentModes.NativeAot,
            projectDocument,
            hostProject.ProjectPath,
            publishProfileDocument,
            hostProject.PublishProfilePath);

        AddGeneratedAppDeploymentModeCheck(
            checks,
            generatedAppRootPath,
            "Generated app single-file posture",
            "PublishSingleFile",
            supportContract.DeploymentModes.SingleFile,
            projectDocument,
            hostProject.ProjectPath,
            publishProfileDocument,
            hostProject.PublishProfilePath);
    }

    private static void EvaluateGeneratedHostBootstrapBaseline(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(hostProject.ProgramPath))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host bootstrap source baseline",
                $"Missing generated host bootstrap source: {ToDisplayRelativePath(generatedAppRootPath, hostProject.ProgramPath)}.",
                "Restore the generated Program.cs file or regenerate the app before teams rely on the scaffolded Cephalon host bootstrap."));
        }
        else if (TryReadGeneratedTextAsset(
                     hostProject.ProgramPath,
                     generatedAppRootPath,
                     "Generated host bootstrap source baseline",
                     "Fix the generated Program.cs file before rerunning `cephalon doctor --app-root`.",
                     checks,
                     out var programContents))
        {
            var missingRequirements = new List<string>(
                RequiredGeneratedHostBootstrapProgramMarkers
                .Where(marker => programContents.IndexOf(marker.Snippet, StringComparison.Ordinal) < 0)
                .Select(marker => marker.Requirement)
                .ToArray());

            if (programContents.IndexOf("builder.AddCephalon();", StringComparison.Ordinal) < 0 &&
                programContents.IndexOf("builder.AddCephalon(engine =>", StringComparison.Ordinal) < 0)
            {
                missingRequirements.Add("AddCephalon");
            }

            if (GeneratedAppUsesBehaviorHttpBootstrap(generatedAppRootPath))
            {
                missingRequirements.AddRange(
                    RequiredGeneratedHostBehaviorBootstrapProgramMarkers
                        .Where(marker => programContents.IndexOf(marker.Snippet, StringComparison.Ordinal) < 0)
                        .Select(marker => marker.Requirement));
            }

            if (missingRequirements.Count > 0)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated host bootstrap source baseline",
                    $"{ToDisplayRelativePath(generatedAppRootPath, hostProject.ProgramPath)} no longer keeps the generated Cephalon host bootstrap explicit for: {string.Join(", ", missingRequirements.Distinct(StringComparer.Ordinal))}.",
                    "Restore the generated Program.cs file so AddCephalonProjectConfigurations, behavior and observability wiring, and MapCephalon stay explicit in the scaffolded host bootstrap."));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Generated host bootstrap source baseline",
                    $"{ToDisplayRelativePath(generatedAppRootPath, hostProject.ProgramPath)} keeps the generated Cephalon host bootstrap explicit with AddCephalonProjectConfigurations, behavior and observability wiring, and MapCephalon().",
                    null));
            }
        }

        XDocument projectDocument;
        try
        {
            projectDocument = XDocument.Load(hostProject.ProjectPath);
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host project baseline",
                $"Could not inspect {ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)}: {exception.Message}",
                "Fix the generated host project file before rerunning `cephalon doctor --app-root`."));
            return;
        }

        var packageReferences = projectDocument
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal))
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var missingPackageReferences = RequiredGeneratedHostProjectPackageReferences
            .Except(packageReferences, StringComparer.Ordinal)
            .ToArray();

        var generatedConfigurationsContentItem = projectDocument
            .Descendants()
            .FirstOrDefault(element =>
                string.Equals(element.Name.LocalName, "Content", StringComparison.Ordinal) &&
                string.Equals(
                    NormalizeMsBuildPath((string?)element.Attribute("Include") ?? (string?)element.Attribute("Update")),
                    GeneratedConfigurationsContentPath,
                    StringComparison.Ordinal));

        var copyToOutputDirectory = generatedConfigurationsContentItem?
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, "CopyToOutputDirectory", StringComparison.Ordinal))
            ?.Value
            ?.Trim();
        var copyToPublishDirectory = generatedConfigurationsContentItem?
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, "CopyToPublishDirectory", StringComparison.Ordinal))
            ?.Value
            ?.Trim();

        var baselineIssues = new List<string>();
        if (missingPackageReferences.Length > 0)
        {
            baselineIssues.Add($"missing package references: {string.Join(", ", missingPackageReferences)}");
        }

        if (generatedConfigurationsContentItem is null)
        {
            baselineIssues.Add($"missing Content Include/Update=\"{GeneratedConfigurationsContentPath}\"");
        }
        else
        {
            if (!string.Equals(copyToOutputDirectory, PreserveNewestValue, StringComparison.Ordinal))
            {
                baselineIssues.Add($"missing CopyToOutputDirectory={PreserveNewestValue}");
            }

            if (!string.Equals(copyToPublishDirectory, PreserveNewestValue, StringComparison.Ordinal))
            {
                baselineIssues.Add($"missing CopyToPublishDirectory={PreserveNewestValue}");
            }
        }

        if (baselineIssues.Count > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host project baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)} no longer keeps the generated package references or `{GeneratedConfigurationsContentPath}` copy/publish baseline explicit ({string.Join("; ", baselineIssues)}).",
                "Restore the generated host project package references and Configurations/**/*.json copy/publish items before teams rely on scaffolded runtime and deployment defaults."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated host project baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)} keeps the generated package references and `{GeneratedConfigurationsContentPath}` copy/publish baseline explicit.",
            null));
    }

    private static bool GeneratedAppUsesBehaviorHttpBootstrap(string generatedAppRootPath)
    {
        var srcRootPath = Path.Combine(generatedAppRootPath, "src");
        if (!Directory.Exists(srcRootPath))
        {
            foreach (var projectPath in Directory.EnumerateFiles(generatedAppRootPath, "*.csproj", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (File.ReadAllText(projectPath).Contains("Cephalon.Behaviors.Http", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        foreach (var projectPath in Directory.EnumerateFiles(srcRootPath, "*.csproj", SearchOption.AllDirectories))
        {
            try
            {
                if (File.ReadAllText(projectPath).Contains("Cephalon.Behaviors.Http", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            catch
            {
                // Fall back to other generated projects when one file cannot be inspected.
            }
        }

        return false;
    }

    private static void EvaluateGeneratedTestHarnessBaseline(
        IReadOnlyList<GeneratedTestProject> testProjects,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (testProjects.Count == 0)
        {
            return;
        }

        var baselineIssues = new List<string>();
        var alignedProjectDetails = new List<string>();
        var encounteredReadFailure = false;

        foreach (var testProject in testProjects)
        {
            var projectIsAligned = true;

            if (!File.Exists(testProject.CompositionSmokeTestPath))
            {
                baselineIssues.Add($"missing generated composition smoke test {ToDisplayRelativePath(generatedAppRootPath, testProject.CompositionSmokeTestPath)}");
            }
            else if (!TryReadGeneratedTextAsset(
                    testProject.CompositionSmokeTestPath,
                    generatedAppRootPath,
                    "Generated test harness baseline",
                    "Fix the generated composition smoke test before rerunning `cephalon doctor --app-root`.",
                    checks,
                    out var compositionSmokeTestContents))
            {
                encounteredReadFailure = true;
                projectIsAligned = false;
            }
            else if (!compositionSmokeTestContents.Contains(GeneratedCompositionSmokeTestMethodMarker, StringComparison.Ordinal))
            {
                baselineIssues.Add($"{ToDisplayRelativePath(generatedAppRootPath, testProject.CompositionSmokeTestPath)} no longer keeps the generated composition smoke placeholder explicit");
                projectIsAligned = false;
            }

            if (!Directory.Exists(testProject.FeaturesDirectoryPath))
            {
                baselineIssues.Add($"missing generated feature specifications under {ToDisplayRelativePath(generatedAppRootPath, testProject.FeaturesDirectoryPath)}");
                projectIsAligned = false;
            }
            else
            {
                var featureSpecificationPaths = Directory
                    .GetFiles(testProject.FeaturesDirectoryPath, GeneratedBehaviorSpecificationSearchPattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (featureSpecificationPaths.Length == 0)
                {
                    baselineIssues.Add($"missing generated feature specifications under {ToDisplayRelativePath(generatedAppRootPath, testProject.FeaturesDirectoryPath)}");
                    projectIsAligned = false;
                }
                else
                {
                    foreach (var featureSpecificationPath in featureSpecificationPaths)
                    {
                        if (!TryReadGeneratedTextAsset(
                                featureSpecificationPath,
                                generatedAppRootPath,
                                "Generated test harness baseline",
                                "Fix the generated behavior specification placeholder before rerunning `cephalon doctor --app-root`.",
                                checks,
                                out var behaviorSpecificationContents))
                        {
                            encounteredReadFailure = true;
                            projectIsAligned = false;
                            continue;
                        }

                        if (!behaviorSpecificationContents.Contains(GeneratedBehaviorSpecificationGivenMarker, StringComparison.Ordinal) ||
                            !behaviorSpecificationContents.Contains(GeneratedBehaviorSpecificationPlaceholderMarker, StringComparison.Ordinal))
                        {
                            baselineIssues.Add($"{ToDisplayRelativePath(generatedAppRootPath, featureSpecificationPath)} no longer keeps the generated Given/When/Then placeholder explicit");
                            projectIsAligned = false;
                        }
                    }

                    if (projectIsAligned)
                    {
                        alignedProjectDetails.Add(
                            $"{ToDisplayRelativePath(generatedAppRootPath, testProject.CompositionSmokeTestPath)} plus {featureSpecificationPaths.Length} feature specification placeholder(s) keep the generated composition and Given/When/Then test harness explicit.");
                    }
                }
            }
        }

        if (baselineIssues.Count > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated test harness baseline",
                string.Join("; ", baselineIssues) + ".",
                "Restore the scaffolded CompositionSmokeTests.cs and Features/*BehaviorSpecifications.cs placeholders before teams rely on the generated test harness."));
            return;
        }

        if (encounteredReadFailure)
        {
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated test harness baseline",
            string.Join("; ", alignedProjectDetails),
            null));
    }

    private static void EvaluateGeneratedAppDeploymentAssets(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        string? solutionPath,
        DeploymentModeSupportContract? supportContract,
        ICollection<DoctorCheck> checks)
    {
        EvaluateGeneratedLocalOrchestrationAssets(generatedAppRootPath, checks);
        EvaluateGeneratedComposeBaseline(generatedAppRootPath, checks);
        EvaluateGeneratedOtelCollectorBaseline(generatedAppRootPath, checks);

        var missingRelativePaths = RequiredGeneratedContainerDeploymentAssetRelativePaths
            .Where(relativePath => !File.Exists(Path.Combine(generatedAppRootPath, relativePath)))
            .Select(relativePath => ToDisplayRelativePath(generatedAppRootPath, Path.Combine(generatedAppRootPath, relativePath)))
            .ToArray();

        if (missingRelativePaths.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated deployment assets",
                $"Missing generated deployment assets: {string.Join(", ", missingRelativePaths)}.",
                "Restore the generated Dockerfile plus container deployment assets or regenerate the app before replaying container-image, Azure Container Apps, or Kubernetes flows."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated deployment assets",
                "./Dockerfile plus container-image, Azure Container Apps, and Kubernetes deployment assets are present.",
                null));
        }

        var dockerfilePath = Path.Combine(generatedAppRootPath, "Dockerfile");
        if (!File.Exists(dockerfilePath))
        {
            return;
        }

        XDocument projectDocument;
        try
        {
            projectDocument = XDocument.Load(hostProject.ProjectPath);
        }
        catch
        {
            return;
        }

        var targetFrameworks = ExtractTargetFrameworks(projectDocument);
        var deploymentBaseline = ResolveGeneratedDeploymentBaseline(targetFrameworks, supportContract);
        if (deploymentBaseline is null)
        {
            return;
        }

        string[] dockerfileLines;
        try
        {
            dockerfileLines = File.ReadAllLines(dockerfilePath);
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated Dockerfile baseline",
                $"Could not inspect {ToDisplayRelativePath(generatedAppRootPath, dockerfilePath)}: {exception.Message}",
                "Fix the generated Dockerfile before rerunning `cephalon doctor --app-root`."));
            return;
        }

        var sdkTag = ExtractDotNetDockerImageTag(dockerfileLines, DotNetSdkDockerImagePrefix);
        var aspNetTag = ExtractDotNetDockerImageTag(dockerfileLines, DotNetAspNetDockerImagePrefix);
        var dockerfileDisplayPath = ToDisplayRelativePath(generatedAppRootPath, dockerfilePath);
        if (string.IsNullOrWhiteSpace(sdkTag) || string.IsNullOrWhiteSpace(aspNetTag))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated Dockerfile baseline",
                $"{dockerfileDisplayPath} does not keep the generated `mcr.microsoft.com/dotnet/sdk:*` and `mcr.microsoft.com/dotnet/aspnet:*` base images intact.",
                "Restore the scaffolded Dockerfile baseline or retarget it deliberately before relying on generated container deployment assets."));
            return;
        }

        if (!string.Equals(sdkTag, deploymentBaseline.ExpectedImageTag, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(aspNetTag, deploymentBaseline.ExpectedImageTag, StringComparison.OrdinalIgnoreCase))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated Dockerfile baseline",
                $"{dockerfileDisplayPath} uses sdk:{sdkTag} and aspnet:{aspNetTag}, but {ToDisplayRelativePath(generatedAppRootPath, hostProject.ProjectPath)} targets {deploymentBaseline.TargetFramework}.",
                $"Restore the Dockerfile base images to `{deploymentBaseline.ExpectedImageTag}` or retarget the generated host project before replaying container deployment flows."));
            return;
        }

        checks.Add(new DoctorCheck(
            deploymentBaseline.AlignedSeverity,
            "Generated Dockerfile baseline",
            $"{dockerfileDisplayPath} uses sdk:{sdkTag} and aspnet:{aspNetTag} {deploymentBaseline.AlignedDetail}.",
            deploymentBaseline.AlignedGuidance));

        var generatedAppId = ResolveGeneratedAppId(solutionPath, generatedAppRootPath, hostProject);
        EvaluateGeneratedContainerDeploymentScriptBaselines(hostProject, generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedKubernetesManifestBaselines(generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedPublishedDeploymentAssets(generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedWindowsServiceBaseline(hostProject, generatedAppRootPath, checks);
        EvaluateGeneratedIisBaseline(generatedAppRootPath, generatedAppId, checks);
        EvaluateAzureAppServiceBaseline(hostProject, generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedLinuxSystemdBaseline(hostProject, generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedWindowsServiceTeardownBaseline(generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedIisTeardownBaseline(generatedAppRootPath, generatedAppId, checks);
        EvaluateGeneratedLinuxSystemdEnvironmentBaseline(generatedAppRootPath, generatedAppId, checks);
    }

    private static void EvaluateGeneratedGuidanceDocsBaseline(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        string? solutionPath,
        ICollection<DoctorCheck> checks)
    {
        var generatedAppId = ResolveGeneratedAppId(solutionPath, generatedAppRootPath, hostProject);
        var rootGuidePath = Path.Combine(generatedAppRootPath, "README.md");
        var localPackageFeedGuidePath = Path.Combine(generatedAppRootPath, ".cephalon", "packages", "README.md");
        var configurationGuidePath = Path.Combine(hostProject.DirectoryPath, "Configurations", "README.md");
        var windowsServiceGuidePath = Path.Combine(generatedAppRootPath, "deploy", "windows-service", "README.md");
        var iisGuidePath = Path.Combine(generatedAppRootPath, "deploy", "iis", "README.md");
        var azureAppServiceGuidePath = Path.Combine(generatedAppRootPath, "deploy", "azure-app-service", "README.md");
        var containerImageGuidePath = Path.Combine(generatedAppRootPath, "deploy", "container-image", "README.md");
        var azureContainerAppsGuidePath = Path.Combine(generatedAppRootPath, "deploy", "azure-container-apps", "README.md");
        var kubernetesGuidePath = Path.Combine(generatedAppRootPath, "deploy", "kubernetes", "README.md");
        var linuxSystemdGuidePath = Path.Combine(generatedAppRootPath, "deploy", "linux", "systemd", "README.md");

        var guidanceDocPaths = new[]
        {
            rootGuidePath,
            localPackageFeedGuidePath,
            configurationGuidePath,
            windowsServiceGuidePath,
            iisGuidePath,
            azureAppServiceGuidePath,
            containerImageGuidePath,
            azureContainerAppsGuidePath,
            kubernetesGuidePath,
            linuxSystemdGuidePath
        };

        var missingRelativePaths = guidanceDocPaths
            .Where(path => !File.Exists(path))
            .Select(path => ToDisplayRelativePath(generatedAppRootPath, path))
            .ToArray();

        if (missingRelativePaths.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated guidance docs assets",
                $"Missing generated guidance docs assets: {string.Join(", ", missingRelativePaths)}.",
                "Restore the generated README guidance assets or regenerate the app before teams follow the scaffolded run, publish, or deployment instructions."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated guidance docs assets",
                $"{ToDisplayRelativePath(generatedAppRootPath, rootGuidePath)}, {ToDisplayRelativePath(generatedAppRootPath, localPackageFeedGuidePath)}, {ToDisplayRelativePath(generatedAppRootPath, configurationGuidePath)}, and deploy/*/README.md guidance assets are present.",
                null));
        }

        EvaluateGeneratedGuideBaseline(
            rootGuidePath,
            generatedAppRootPath,
            "Generated root guidance baseline",
            RequiredGeneratedRootGuideMarkers.Append(generatedAppId).ToArray(),
            $"{ToDisplayRelativePath(generatedAppRootPath, rootGuidePath)} keeps generated package-source, split-config, publish, deployment, and local-orchestration guidance explicit for {generatedAppId}.",
            "Restore the generated root README so the scaffolded adoption path stays explicit before teams edit the app or replay deployment flows.",
            checks);

        EvaluateGeneratedGuideBaseline(
            localPackageFeedGuidePath,
            generatedAppRootPath,
            "Generated local package feed guidance baseline",
            RequiredGeneratedLocalPackageFeedGuideMarkers,
            $"{ToDisplayRelativePath(generatedAppRootPath, localPackageFeedGuidePath)} keeps generated local package-feed bootstrap, publish-package-artifacts.ps1, and shared-feed replacement guidance explicit.",
            "Restore the generated .cephalon/packages/README.md file so local package-feed bootstrap guidance stays explicit before teams seed packages or replace the cephalon package source.",
            checks);

        EvaluateGeneratedGuideBaseline(
            configurationGuidePath,
            generatedAppRootPath,
            "Generated configuration guidance baseline",
            RequiredGeneratedConfigurationGuideMarkers,
            $"{ToDisplayRelativePath(generatedAppRootPath, configurationGuidePath)} keeps generated Add*.json, grouped override, and AddCephalonProjectConfigurations() guidance explicit.",
            "Restore the generated Configurations/README.md file so split project configuration guidance stays explicit for external adopters.",
            checks);

        EvaluateGeneratedGuideBaseline(
            windowsServiceGuidePath,
            generatedAppRootPath,
            "Generated Windows Service guide baseline",
            ["install-service.ps1", "remove-service.ps1", "CephalonFolder.pubxml", generatedAppId],
            $"{ToDisplayRelativePath(generatedAppRootPath, windowsServiceGuidePath)} keeps generated Windows Service publish, install, and removal guidance explicit for {generatedAppId}.",
            "Restore the generated Windows Service README so the published-output and service-manager guidance stays aligned with the current app root.",
            checks);

        EvaluateGeneratedGuideBaseline(
            iisGuidePath,
            generatedAppRootPath,
            "Generated IIS guide baseline",
            ["install-site.ps1", "remove-site.ps1", "web.config", generatedAppId],
            $"{ToDisplayRelativePath(generatedAppRootPath, iisGuidePath)} keeps generated IIS publish, install, and removal guidance explicit for {generatedAppId}.",
            "Restore the generated IIS README so the hosted Windows site/app-pool guidance stays aligned with the current app root.",
            checks);

        EvaluateGeneratedGuideBaseline(
            azureAppServiceGuidePath,
            generatedAppRootPath,
            "Generated Azure App Service guide baseline",
            ["deploy-zip.ps1", "azure-app-service.zip", generatedAppId],
            $"{ToDisplayRelativePath(generatedAppRootPath, azureAppServiceGuidePath)} keeps generated Azure App Service publish and ZIP-deploy guidance explicit for {generatedAppId}.",
            "Restore the generated Azure App Service README so the ZIP packaging and deploy guidance stays aligned with the current app root.",
            checks);

        EvaluateGeneratedGuideBaseline(
            containerImageGuidePath,
            generatedAppRootPath,
            "Generated container image guide baseline",
            ["publish-image.ps1", "Dockerfile", "docker login", "-Push"],
            $"{ToDisplayRelativePath(generatedAppRootPath, containerImageGuidePath)} keeps generated Dockerfile build and publish-image.ps1 guidance explicit.",
            "Restore the generated container-image README so the provider-neutral build, tag, and push guidance stays aligned with the current app root.",
            checks);

        EvaluateGeneratedGuideBaseline(
            azureContainerAppsGuidePath,
            generatedAppRootPath,
            "Generated Azure Container Apps guide baseline",
            ["deploy-up.ps1", "Dockerfile", "az containerapp up", "--source"],
            $"{ToDisplayRelativePath(generatedAppRootPath, azureContainerAppsGuidePath)} keeps generated Dockerfile source-deploy guidance explicit through deploy-up.ps1.",
            "Restore the generated Azure Container Apps README so the hosted source-deploy guidance stays aligned with the current app root.",
            checks);

        EvaluateGeneratedGuideBaseline(
            kubernetesGuidePath,
            generatedAppRootPath,
            "Generated Kubernetes guide baseline",
            ["apply.ps1", "kustomization.yaml", "deployment.yaml", "service.yaml", "kubectl kustomize"],
            $"{ToDisplayRelativePath(generatedAppRootPath, kubernetesGuidePath)} keeps generated apply.ps1, kustomization.yaml, deployment.yaml, and service.yaml guidance explicit.",
            "Restore the generated Kubernetes README so the manifest preview and apply guidance stays aligned with the current app root.",
            checks);

        EvaluateGeneratedGuideBaseline(
            linuxSystemdGuidePath,
            generatedAppRootPath,
            "Generated Linux systemd guide baseline",
            [$"{generatedAppId}.service", $"{generatedAppId}.env", generatedAppId, "systemctl"],
            $"{ToDisplayRelativePath(generatedAppRootPath, linuxSystemdGuidePath)} keeps generated Linux service-manager guidance explicit for {generatedAppId}.",
            "Restore the generated Linux systemd README so the published-output and service-manager guidance stays aligned with the current app root.",
            checks);
    }

    private static void EvaluateGeneratedGuideBaseline(
        string guidePath,
        string generatedAppRootPath,
        string title,
        IReadOnlyList<string> requiredSnippets,
        string successDetail,
        string failureGuidance,
        ICollection<DoctorCheck> checks)
    {
        EvaluateGeneratedTextAssetBaseline(
            guidePath,
            generatedAppRootPath,
            title,
            requiredSnippets,
            successDetail,
            "no longer keeps explicit generated guidance for",
            failureGuidance,
            checks);
    }

    private static void EvaluateGeneratedContainerDeploymentScriptBaselines(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var generatedContainerResourceName = BuildGeneratedKubernetesResourceName(generatedAppId);
        var generatedContainerImagePlaceholder = BuildGeneratedContainerImagePlaceholder(generatedAppId);
        var generatedContainerAppName = BuildGeneratedAzureContainerAppName(generatedAppId);
        var generatedHostProjectName = Path.GetFileNameWithoutExtension(hostProject.ProjectPath);
        var generatedHostProjectRelativePath = Path.GetRelativePath(generatedAppRootPath, hostProject.ProjectPath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        EvaluateGeneratedScriptBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "container-image", "publish-image.ps1"),
            generatedAppRootPath,
            "Generated container image script baseline",
            RequiredGeneratedContainerImageScriptMarkers
                .Append(generatedContainerImagePlaceholder)
                .ToArray(),
            $"./deploy/container-image/publish-image.ps1 keeps the generated Dockerfile, NuGet.config, image placeholder, and preview/push flow explicit for {generatedAppId}.",
            "Restore the generated container-image publish script so the provider-neutral build, preview, and push flow stays aligned with the current app root.",
            checks);

        EvaluateGeneratedScriptBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "azure-container-apps", "deploy-up.ps1"),
            generatedAppRootPath,
            "Generated Azure Container Apps script baseline",
            RequiredGeneratedAzureContainerAppsScriptMarkers
                .Append(generatedContainerAppName)
                .Append(generatedHostProjectRelativePath)
                .ToArray(),
            $"./deploy/azure-container-apps/deploy-up.ps1 keeps the generated source-root, host-project, and az containerapp up defaults explicit for {generatedAppId}.",
            "Restore the generated Azure Container Apps deploy script so the source-deploy flow stays aligned with the current app root and host identity.",
            checks);

        EvaluateGeneratedScriptBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "kubernetes", "apply.ps1"),
            generatedAppRootPath,
            "Generated Kubernetes apply script baseline",
            RequiredGeneratedKubernetesApplyScriptMarkers
                .Append(generatedContainerResourceName)
                .Append(generatedContainerImagePlaceholder)
                .ToArray(),
            $"./deploy/kubernetes/apply.ps1 keeps the generated namespace, image placeholder, manifest root, and kubectl kustomize/apply flow explicit for {generatedAppId}.",
            "Restore the generated Kubernetes apply script so the manifest preview and apply flow stays aligned with the current app root.",
            checks);
    }

    private static void EvaluateGeneratedKubernetesManifestBaselines(
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var generatedContainerResourceName = BuildGeneratedKubernetesResourceName(generatedAppId);
        var generatedContainerImagePlaceholder = BuildGeneratedContainerImagePlaceholder(generatedAppId);

        EvaluateGeneratedTextAssetBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "kubernetes", "kustomization.yaml"),
            generatedAppRootPath,
            "Generated Kubernetes kustomization baseline",
            RequiredGeneratedKubernetesKustomizationManifestMarkers
                .Append($"namespace: {generatedContainerResourceName}")
                .ToArray(),
            $"./deploy/kubernetes/kustomization.yaml keeps the generated namespace plus namespace/deployment/service manifest set explicit for {generatedAppId}.",
            "no longer keeps the generated Kubernetes manifest baseline explicit for",
            "Restore the generated kustomization.yaml file so the Kubernetes manifest-set contract stays aligned with the current app root.",
            checks);

        EvaluateGeneratedTextAssetBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "kubernetes", "namespace.yaml"),
            generatedAppRootPath,
            "Generated Kubernetes namespace baseline",
            RequiredGeneratedKubernetesNamespaceManifestMarkers
                .Append($"name: {generatedContainerResourceName}")
                .Append($"app.kubernetes.io/name: {generatedContainerResourceName}")
                .ToArray(),
            $"./deploy/kubernetes/namespace.yaml keeps the generated namespace identity and labels explicit for {generatedAppId}.",
            "no longer keeps the generated Kubernetes manifest baseline explicit for",
            "Restore the generated namespace.yaml file so the Kubernetes namespace identity and labels stay aligned with the current app root.",
            checks);

        EvaluateGeneratedTextAssetBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "kubernetes", "deployment.yaml"),
            generatedAppRootPath,
            "Generated Kubernetes deployment baseline",
            RequiredGeneratedKubernetesDeploymentManifestMarkers
                .Append($"name: {generatedContainerResourceName}")
                .Append($"app.kubernetes.io/name: {generatedContainerResourceName}")
                .Append(generatedContainerImagePlaceholder)
                .ToArray(),
            $"./deploy/kubernetes/deployment.yaml keeps the generated container image, env, probe, and resource contract explicit for {generatedAppId}.",
            "no longer keeps the generated Kubernetes manifest baseline explicit for",
            "Restore the generated deployment.yaml file so the Kubernetes host workload contract stays aligned with the current app root.",
            checks);

        EvaluateGeneratedTextAssetBaseline(
            Path.Combine(generatedAppRootPath, "deploy", "kubernetes", "service.yaml"),
            generatedAppRootPath,
            "Generated Kubernetes service baseline",
            RequiredGeneratedKubernetesServiceManifestMarkers
                .Append($"name: {generatedContainerResourceName}")
                .Append($"app.kubernetes.io/name: {generatedContainerResourceName}")
                .ToArray(),
            $"./deploy/kubernetes/service.yaml keeps the generated ClusterIP service contract explicit for {generatedAppId}.",
            "no longer keeps the generated Kubernetes manifest baseline explicit for",
            "Restore the generated service.yaml file so the Kubernetes service contract stays aligned with the current app root.",
            checks);
    }

    private static void EvaluateGeneratedScriptBaseline(
        string scriptPath,
        string generatedAppRootPath,
        string title,
        IReadOnlyList<string> requiredSnippets,
        string successDetail,
        string failureGuidance,
        ICollection<DoctorCheck> checks)
    {
        EvaluateGeneratedTextAssetBaseline(
            scriptPath,
            generatedAppRootPath,
            title,
            requiredSnippets,
            successDetail,
            "no longer keeps the generated deployment-script baseline explicit for",
            failureGuidance,
            checks);
    }

    private static void EvaluateGeneratedTextAssetBaseline(
        string assetPath,
        string generatedAppRootPath,
        string title,
        IReadOnlyList<string> requiredSnippets,
        string successDetail,
        string failureDetailPrefix,
        string failureGuidance,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(assetPath))
        {
            return;
        }

        var assetDisplayPath = ToDisplayRelativePath(generatedAppRootPath, assetPath);
        if (!TryReadGeneratedTextAsset(
                assetPath,
                generatedAppRootPath,
                title,
                $"Fix {assetDisplayPath} before rerunning `cephalon doctor --app-root`.",
                checks,
                out var assetContents))
        {
            return;
        }

        var missingSnippets = requiredSnippets
            .Where(snippet => assetContents.IndexOf(snippet, StringComparison.OrdinalIgnoreCase) < 0)
            .ToArray();

        if (missingSnippets.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                title,
                $"{assetDisplayPath} {failureDetailPrefix}: {string.Join(", ", missingSnippets)}.",
                failureGuidance));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            title,
            successDetail,
            null));
    }

    private static void EvaluateGeneratedDocumentationSurfaceAssets(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var openApiSettingsPath = Path.Combine(hostProject.DirectoryPath, "Configurations", "AddOpenApi.json");
        var referenceDocsSettingsPath = Path.Combine(hostProject.DirectoryPath, "Configurations", "AddReferenceDocs.json");

        var missingRelativePaths = new[]
            {
                openApiSettingsPath,
                referenceDocsSettingsPath
            }
            .Where(path => !File.Exists(path))
            .Select(path => ToDisplayRelativePath(generatedAppRootPath, path))
            .ToArray();

        if (missingRelativePaths.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated documentation surface assets",
                $"Missing generated documentation surface assets: {string.Join(", ", missingRelativePaths)}.",
                "Restore the generated OpenAPI and hosted reference-doc config assets or regenerate the app before teams rely on `/scalar` or hosted reference-doc routes."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated documentation surface assets",
                $"{ToDisplayRelativePath(generatedAppRootPath, openApiSettingsPath)} and {ToDisplayRelativePath(generatedAppRootPath, referenceDocsSettingsPath)} are present.",
                null));
        }

        EvaluateGeneratedOpenApiBaseline(openApiSettingsPath, generatedAppRootPath, checks);
        EvaluateGeneratedReferenceDocsBaseline(referenceDocsSettingsPath, generatedAppRootPath, checks);
    }

    private static void EvaluateGeneratedSplitConfigurationAssets(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var configurationDirectoryPath = Path.Combine(hostProject.DirectoryPath, "Configurations");
        var appModelSettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.AppModel.json");
        var dataSettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Data.json");
        var identitySettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Identity.json");
        var tenancySettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Tenancy.json");
        var auditSettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Audit.json");
        var messagingSettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Messaging.json");
        var observabilitySettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Observability.json");
        var localizationSettingsPath = Path.Combine(configurationDirectoryPath, "AddEngine.Localization.json");
        var developmentObservabilitySettingsPath = Path.Combine(configurationDirectoryPath, "Observability", "Development.json");

        var missingRelativePaths = RequiredGeneratedSplitConfigurationAssetRelativePaths
            .Select(relativePath => Path.Combine(hostProject.DirectoryPath, relativePath))
            .Where(path => !File.Exists(path))
            .Select(path => ToDisplayRelativePath(generatedAppRootPath, path))
            .ToArray();

        var configurationDirectoryDisplayPath = ToDisplayRelativePath(generatedAppRootPath, configurationDirectoryPath);
        var developmentObservabilityDisplayPath = ToDisplayRelativePath(generatedAppRootPath, developmentObservabilitySettingsPath);
        if (missingRelativePaths.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated split configuration assets",
                $"Missing generated split configuration assets: {string.Join(", ", missingRelativePaths)}.",
                "Restore the generated AddEngine.*.json files plus Configurations/Observability/Development.json or regenerate the app before teams rely on split project configuration defaults."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated split configuration assets",
                $"{configurationDirectoryDisplayPath}/AddEngine.*.json and {developmentObservabilityDisplayPath} are present.",
                null));
        }

        EvaluateGeneratedAppModelSplitConfigurationBaseline(appModelSettingsPath, generatedAppRootPath, checks);
        EvaluateGeneratedEngineFeatureSplitConfigurationBaseline(
            dataSettingsPath,
            identitySettingsPath,
            tenancySettingsPath,
            auditSettingsPath,
            messagingSettingsPath,
            generatedAppRootPath,
            checks);
        EvaluateGeneratedObservabilitySplitConfigurationBaseline(observabilitySettingsPath, generatedAppRootPath, checks);
        EvaluateGeneratedLocalizationSplitConfigurationBaseline(localizationSettingsPath, generatedAppRootPath, checks);
        EvaluateGeneratedDevelopmentObservabilityBaseline(developmentObservabilitySettingsPath, generatedAppRootPath, checks);
    }

    private static void EvaluateGeneratedAppModelSplitConfigurationBaseline(
        string appModelSettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(appModelSettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                appModelSettingsPath,
                generatedAppRootPath,
                "Generated app-model split-config baseline",
                "Fix the generated AddEngine.AppModel.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var appModelRoot))
        {
            return;
        }

        if (!TryGetJsonObject(appModelRoot, "Engine", out var engineSection) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(engineSection, "Blueprint")) ||
            !TryGetJsonObject(engineSection, "Discovery", out var discoverySection) ||
            !TryGetJsonArray(discoverySection, "Assemblies", out var assemblies) ||
            !JsonArrayContainsOnlyStrings(assemblies) ||
            !TryGetJsonArray(engineSection, "Patterns", out var patterns) ||
            !JsonArrayContainsOnlyStrings(patterns) ||
            !TryGetJsonArray(engineSection, "Technologies", out var technologies) ||
            !JsonArrayContainsOnlyStrings(technologies) ||
            !TryGetJsonArray(engineSection, "Transports", out var transports) ||
            !JsonArrayContainsOnlyStrings(transports))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated app-model split-config baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, appModelSettingsPath)} no longer keeps explicit Engine blueprint, discovery assemblies, pattern, technology, and transport selections.",
                "Restore the generated AddEngine.AppModel.json file so the scaffolded app model stays explicit in split project configuration."));
            return;
        }

        var blueprint = GetRequiredJsonString(engineSection, "Blueprint");
        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated app-model split-config baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, appModelSettingsPath)} keeps explicit Engine app-model selections with Blueprint={blueprint} and {assemblies.Count} discovery assembly entries.",
            null));
    }

    private static void EvaluateGeneratedEngineFeatureSplitConfigurationBaseline(
        string dataSettingsPath,
        string identitySettingsPath,
        string tenancySettingsPath,
        string auditSettingsPath,
        string messagingSettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var splitConfigFiles = new (string Path, string SectionName)[]
        {
            (dataSettingsPath, "Data"),
            (identitySettingsPath, "Identity"),
            (tenancySettingsPath, "Tenancy"),
            (messagingSettingsPath, "Messaging")
        };

        foreach (var splitConfigFile in splitConfigFiles)
        {
            if (!File.Exists(splitConfigFile.Path))
            {
                return;
            }

            if (!TryReadGeneratedJsonObjectAsset(
                    splitConfigFile.Path,
                    generatedAppRootPath,
                    "Generated engine feature split-config baseline",
                    $"Fix the generated {Path.GetFileName(splitConfigFile.Path)} file before rerunning `cephalon doctor --app-root`.",
                    checks,
                    out var splitConfigRoot))
            {
                return;
            }

            if (!TryGetJsonObject(splitConfigRoot, "Engine", out var engineSection) ||
                !TryGetJsonObject(engineSection, splitConfigFile.SectionName, out _))
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated engine feature split-config baseline",
                    $"{ToDisplayRelativePath(generatedAppRootPath, splitConfigFile.Path)} no longer keeps an explicit `Engine:{splitConfigFile.SectionName}` section.",
                    $"Restore the generated {Path.GetFileName(splitConfigFile.Path)} file so split project configuration keeps `{splitConfigFile.SectionName}` explicit."));
                return;
            }
        }

        if (!File.Exists(auditSettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                auditSettingsPath,
                generatedAppRootPath,
                "Generated engine feature split-config baseline",
                "Fix the generated AddEngine.Audit.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var auditRoot))
        {
            return;
        }

        if (!TryGetJsonObject(auditRoot, "Engine", out var auditEngineSection) ||
            !TryGetJsonObject(auditEngineSection, "Audit", out var auditSection) ||
            !TryGetRequiredBoolean(auditSection, "Enabled", out var auditEnabled))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated engine feature split-config baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, auditSettingsPath)} no longer keeps an explicit boolean `Engine:Audit:Enabled` baseline.",
                "Restore the generated AddEngine.Audit.json file so audit enablement stays explicit in split project configuration."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated engine feature split-config baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, dataSettingsPath)}, {ToDisplayRelativePath(generatedAppRootPath, identitySettingsPath)}, {ToDisplayRelativePath(generatedAppRootPath, tenancySettingsPath)}, {ToDisplayRelativePath(generatedAppRootPath, auditSettingsPath)}, and {ToDisplayRelativePath(generatedAppRootPath, messagingSettingsPath)} keep explicit Engine data, identity, tenancy, audit, and messaging sections with Audit.Enabled={auditEnabled.ToString().ToLowerInvariant()}.",
            null));
    }

    private static void EvaluateGeneratedObservabilitySplitConfigurationBaseline(
        string observabilitySettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(observabilitySettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                observabilitySettingsPath,
                generatedAppRootPath,
                "Generated observability split-config baseline",
                "Fix the generated AddEngine.Observability.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var observabilityRoot))
        {
            return;
        }

        if (!TryGetJsonObject(observabilityRoot, "Engine", out var engineSection) ||
            !TryGetJsonObject(engineSection, "Observability", out var observabilitySection) ||
            !TryGetRequiredBoolean(observabilitySection, "LogManifestSummary", out _) ||
            !TryGetRequiredBoolean(observabilitySection, "LogModuleSummary", out _) ||
            !TryGetRequiredBoolean(observabilitySection, "LogCapabilitySummary", out _) ||
            !TryGetJsonObject(observabilitySection, "Telemetry", out var telemetrySection) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(telemetrySection, "Provider")) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(telemetrySection, "Protocol")) ||
            !TryGetRequiredBoolean(telemetrySection, "ExportLogs", out var exportLogs) ||
            !TryGetRequiredBoolean(telemetrySection, "ExportMetrics", out var exportMetrics) ||
            !TryGetRequiredBoolean(telemetrySection, "ExportTraces", out var exportTraces))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated observability split-config baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, observabilitySettingsPath)} no longer keeps explicit Engine observability summary and telemetry export settings.",
                "Restore the generated AddEngine.Observability.json file so observability defaults stay explicit in split project configuration."));
            return;
        }

        var provider = GetRequiredJsonString(telemetrySection, "Provider");
        var protocol = GetRequiredJsonString(telemetrySection, "Protocol");
        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated observability split-config baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, observabilitySettingsPath)} keeps explicit Engine observability telemetry defaults with Provider={provider}, Protocol={protocol}, ExportLogs={exportLogs.ToString().ToLowerInvariant()}, ExportMetrics={exportMetrics.ToString().ToLowerInvariant()}, and ExportTraces={exportTraces.ToString().ToLowerInvariant()}.",
            null));
    }

    private static void EvaluateGeneratedLocalizationSplitConfigurationBaseline(
        string localizationSettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(localizationSettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                localizationSettingsPath,
                generatedAppRootPath,
                "Generated localization split-config baseline",
                "Fix the generated AddEngine.Localization.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var localizationRoot))
        {
            return;
        }

        if (!TryGetJsonObject(localizationRoot, "Engine", out var engineSection) ||
            !TryGetJsonObject(engineSection, "Localization", out var localizationSection) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(localizationSection, "DefaultCulture")) ||
            !TryGetJsonArray(localizationSection, "SupportedCultures", out var supportedCultures) ||
            !JsonArrayContainsOnlyStrings(supportedCultures) ||
            !TryGetJsonObject(localizationSection, "Resources", out var resourcesSection) ||
            !TryGetJsonObject(resourcesSection, "th", out _))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated localization split-config baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, localizationSettingsPath)} no longer keeps explicit Engine localization culture and resource defaults.",
                "Restore the generated AddEngine.Localization.json file so localization defaults stay explicit in split project configuration."));
            return;
        }

        var defaultCulture = GetRequiredJsonString(localizationSection, "DefaultCulture");
        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated localization split-config baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, localizationSettingsPath)} keeps explicit Engine localization defaults with DefaultCulture={defaultCulture} and {supportedCultures.Count} supported cultures.",
            null));
    }

    private static void EvaluateGeneratedDevelopmentObservabilityBaseline(
        string developmentObservabilitySettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(developmentObservabilitySettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                developmentObservabilitySettingsPath,
                generatedAppRootPath,
                "Generated development observability baseline",
                "Fix the generated Configurations/Observability/Development.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var developmentObservabilityRoot))
        {
            return;
        }

        if (!TryGetJsonObject(developmentObservabilityRoot, "Serilog", out var serilogSection) ||
            !TryGetJsonArray(serilogSection, "Using", out var usingEntries) ||
            !JsonArrayContainsStringValue(usingEntries, "Serilog.Sinks.Console") ||
            !TryGetJsonObject(serilogSection, "MinimumLevel", out var minimumLevelSection) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(minimumLevelSection, "Default")) ||
            !TryGetJsonObject(minimumLevelSection, "Override", out var overrideSection) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(overrideSection, "Microsoft")) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(overrideSection, "System")) ||
            !TryGetJsonArray(serilogSection, "WriteTo", out var writeToEntries) ||
            !JsonArrayContainsObjectWithString(writeToEntries, "Name", "Console") ||
            !TryGetJsonObject(serilogSection, "Properties", out var propertiesSection) ||
            string.IsNullOrWhiteSpace(GetRequiredJsonString(propertiesSection, "Application")))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated development observability baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, developmentObservabilitySettingsPath)} no longer keeps the generated Serilog console sample explicit for development overrides.",
                "Restore the generated Configurations/Observability/Development.json file so the optional Serilog development override stays explicit in split project configuration."));
            return;
        }

        var applicationName = GetRequiredJsonString(propertiesSection, "Application");
        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated development observability baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, developmentObservabilitySettingsPath)} keeps the generated Serilog console sample explicit with Application={applicationName}.",
            null));
    }

    private static void EvaluateGeneratedLocalOrchestrationAssets(
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var missingRelativePaths = RequiredGeneratedLocalOrchestrationAssetRelativePaths
            .Where(relativePath => !File.Exists(Path.Combine(generatedAppRootPath, relativePath)))
            .Select(relativePath => ToDisplayRelativePath(generatedAppRootPath, Path.Combine(generatedAppRootPath, relativePath)))
            .ToArray();

        if (missingRelativePaths.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated local orchestration assets",
                $"Missing generated local orchestration assets: {string.Join(", ", missingRelativePaths)}.",
                "Restore the generated compose and OTLP collector assets or regenerate the app before replaying the local `docker compose up --build` path."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated local orchestration assets",
            "./compose.yaml and ./otel-collector-config.yaml are present.",
            null));
    }

    private static void EvaluateGeneratedComposeBaseline(
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var composePath = Path.Combine(generatedAppRootPath, "compose.yaml");
        if (!File.Exists(composePath))
        {
            return;
        }

        if (!TryReadGeneratedTextAsset(
                composePath,
                generatedAppRootPath,
                "Generated compose baseline",
                "Fix the generated compose.yaml file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var composeContents))
        {
            return;
        }

        if (!ContainsAllFragments(
                composeContents,
                "dockerfile: Dockerfile",
                "ASPNETCORE_HTTP_PORTS: 8080",
                "DOTNET_ENVIRONMENT: Container",
                "Engine__Observability__Telemetry__Protocol: otlp/http",
                "Engine__Observability__Telemetry__Endpoint: http://otel-collector:4318",
                "depends_on:",
                "- otel-collector",
                "otel/opentelemetry-collector-contrib:",
                "./otel-collector-config.yaml:/etc/otelcol-contrib/config.yaml:ro"))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated compose baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, composePath)} no longer keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.",
                "Restore the generated compose.yaml file so the Dockerfile, container environment, OTLP endpoint, and collector mount stay aligned with the current local runtime path."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated compose baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, composePath)} keeps the generated local container-runtime baseline aligned with Dockerfile, OTLP collector handoff, and the current compose defaults.",
            null));
    }

    private static void EvaluateGeneratedOtelCollectorBaseline(
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var collectorConfigPath = Path.Combine(generatedAppRootPath, "otel-collector-config.yaml");
        if (!File.Exists(collectorConfigPath))
        {
            return;
        }

        if (!TryReadGeneratedTextAsset(
                collectorConfigPath,
                generatedAppRootPath,
                "Generated OpenTelemetry collector baseline",
                "Fix the generated OTLP collector config before rerunning `cephalon doctor --app-root`.",
                checks,
                out var collectorConfigContents))
        {
            return;
        }

        if (!ContainsAllFragments(
                collectorConfigContents,
                "health_check:",
                "endpoint: 0.0.0.0:13133",
                "otlp:",
                "http:",
                "endpoint: 0.0.0.0:4318",
                "batch: {}",
                "debug:",
                "logs:",
                "metrics:",
                "traces:"))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated OpenTelemetry collector baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, collectorConfigPath)} no longer keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.",
                "Restore the generated otel-collector-config.yaml file so the local OTLP handoff and collector health defaults stay aligned with the current container-runtime path."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated OpenTelemetry collector baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, collectorConfigPath)} keeps the generated OTLP collector baseline aligned with health_check, otlp/http on 4318, and debug exporter pipelines.",
            null));
    }

    private static void EvaluateGeneratedOpenApiBaseline(
        string openApiSettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(openApiSettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                openApiSettingsPath,
                generatedAppRootPath,
                "Generated OpenAPI baseline",
                "Fix the generated AddOpenApi.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var openApiRoot))
        {
            return;
        }

        if (openApiRoot["OpenApi"] is not JsonObject openApiSection)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated OpenAPI baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, openApiSettingsPath)} no longer keeps an explicit `OpenApi` section for the generated REST docs surface.",
                "Restore the generated AddOpenApi.json file so `/openapi/*` and `/scalar` configuration stays explicit in the split project settings."));
            return;
        }

        var title = GetRequiredJsonString(openApiSection, "Title");
        if (string.IsNullOrWhiteSpace(title))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated OpenAPI baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, openApiSettingsPath)} no longer keeps an explicit `OpenApi:Title` for the generated REST docs surface.",
                "Restore the generated AddOpenApi.json file so the OpenAPI title stays explicit before teams rely on `/openapi/*` and `/scalar`."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated OpenAPI baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, openApiSettingsPath)} keeps the generated REST docs surface explicit with Title='{title}'.",
            null));
    }

    private static void EvaluateGeneratedReferenceDocsBaseline(
        string referenceDocsSettingsPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        if (!File.Exists(referenceDocsSettingsPath))
        {
            return;
        }

        if (!TryReadGeneratedJsonObjectAsset(
                referenceDocsSettingsPath,
                generatedAppRootPath,
                "Generated hosted reference docs baseline",
                "Fix the generated AddReferenceDocs.json file before rerunning `cephalon doctor --app-root`.",
                checks,
                out var referenceDocsRoot))
        {
            return;
        }

        if (referenceDocsRoot["ReferenceDocs"] is not JsonObject referenceDocsSection)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated hosted reference docs baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, referenceDocsSettingsPath)} no longer keeps an explicit `ReferenceDocs` section for optional hosted API reference output.",
                "Restore the generated AddReferenceDocs.json file so hosted reference-doc settings stay explicit even when the route remains disabled by default."));
            return;
        }

        if (!TryGetRequiredBoolean(referenceDocsSection, "Enabled", out var enabled))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated hosted reference docs baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, referenceDocsSettingsPath)} no longer keeps an explicit boolean `ReferenceDocs:Enabled` setting.",
                "Restore the generated AddReferenceDocs.json file so hosted reference-doc enablement stays explicit."));
            return;
        }

        var routePrefix = GetRequiredJsonString(referenceDocsSection, "RoutePrefix");
        var directoryPath = GetRequiredJsonString(referenceDocsSection, "DirectoryPath");
        var defaultDocument = GetRequiredJsonString(referenceDocsSection, "DefaultDocument");
        if (string.IsNullOrWhiteSpace(routePrefix) ||
            !routePrefix.StartsWith('/') ||
            string.IsNullOrWhiteSpace(directoryPath) ||
            string.IsNullOrWhiteSpace(defaultDocument))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated hosted reference docs baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, referenceDocsSettingsPath)} no longer keeps explicit hosted reference-doc route, directory, and default-document settings.",
                "Restore the generated AddReferenceDocs.json file so hosted reference-doc routing and directory defaults stay explicit before teams turn that route on."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated hosted reference docs baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, referenceDocsSettingsPath)} keeps hosted reference docs explicit with Enabled={enabled.ToString().ToLowerInvariant()}, RoutePrefix={routePrefix}, DirectoryPath={directoryPath}, and DefaultDocument={defaultDocument}.",
            null));
    }

    private static void EvaluateGeneratedPublishedDeploymentAssets(
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var dynamicRelativePaths = new[]
        {
            Path.Combine("deploy", "linux", "systemd", $"{generatedAppId}.service"),
            Path.Combine("deploy", "linux", "systemd", $"{generatedAppId}.env")
        };

        var missingRelativePaths = RequiredGeneratedPublishedDeploymentAssetRelativePaths
            .Concat(dynamicRelativePaths)
            .Where(relativePath => !File.Exists(Path.Combine(generatedAppRootPath, relativePath)))
            .Select(relativePath => ToDisplayRelativePath(generatedAppRootPath, Path.Combine(generatedAppRootPath, relativePath)))
            .ToArray();

        if (missingRelativePaths.Length > 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated self-hosted and hosted deployment assets",
                $"Missing generated self-hosted and hosted deployment assets: {string.Join(", ", missingRelativePaths)}.",
                "Restore the generated Windows Service, IIS, Azure App Service, and Linux systemd deployment assets or regenerate the app before replaying published-output deployment flows."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated self-hosted and hosted deployment assets",
            "./deploy/windows-service, ./deploy/iis, ./deploy/azure-app-service, and ./deploy/linux/systemd assets are present.",
            null));
    }

    private static void EvaluateGeneratedWindowsServiceBaseline(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        var installScriptPath = Path.Combine(generatedAppRootPath, "deploy", "windows-service", "install-service.ps1");
        if (!File.Exists(installScriptPath))
        {
            return;
        }

        if (!TryReadGeneratedTextAsset(
                installScriptPath,
                generatedAppRootPath,
                "Generated Windows Service baseline",
                "Fix the generated Windows Service install script before rerunning `cephalon doctor --app-root`.",
                checks,
                out var installScriptContents))
        {
            return;
        }

        var expectedHostAssemblyName = $"{Path.GetFileNameWithoutExtension(hostProject.ProjectPath)}.dll";
        if (!installScriptContents.Contains(expectedHostAssemblyName, StringComparison.Ordinal) ||
            !installScriptContents.Contains("sc.exe create", StringComparison.Ordinal))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated Windows Service baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, installScriptPath)} no longer references {expectedHostAssemblyName} through the generated Windows Service install flow.",
                "Restore the generated Windows Service install script so the published host DLL stays aligned with the current app root."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated Windows Service baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, installScriptPath)} keeps the generated Windows Service install flow aligned with {expectedHostAssemblyName}.",
            null));
    }

    private static void EvaluateGeneratedIisBaseline(
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var installScriptPath = Path.Combine(generatedAppRootPath, "deploy", "iis", "install-site.ps1");
        if (!File.Exists(installScriptPath))
        {
            return;
        }

        if (!TryReadGeneratedTextAsset(
                installScriptPath,
                generatedAppRootPath,
                "Generated IIS baseline",
                "Fix the generated IIS install script before rerunning `cephalon doctor --app-root`.",
                checks,
                out var installScriptContents))
        {
            return;
        }

        if (!installScriptContents.Contains("web.config", StringComparison.Ordinal) ||
            !installScriptContents.Contains(generatedAppId, StringComparison.Ordinal))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated IIS baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, installScriptPath)} no longer keeps the generated IIS site/app-pool defaults aligned with {generatedAppId}.",
                "Restore the generated IIS install script so the published web.config and physical-path defaults stay aligned with the current app root."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated IIS baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, installScriptPath)} keeps the generated IIS site/app-pool defaults aligned with {generatedAppId}.",
            null));
    }

    private static void EvaluateAzureAppServiceBaseline(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var deployScriptPath = Path.Combine(generatedAppRootPath, "deploy", "azure-app-service", "deploy-zip.ps1");
        if (!File.Exists(deployScriptPath))
        {
            return;
        }

        if (!TryReadGeneratedTextAsset(
                deployScriptPath,
                generatedAppRootPath,
                "Generated Azure App Service baseline",
                "Fix the generated Azure App Service deploy script before rerunning `cephalon doctor --app-root`.",
                checks,
                out var deployScriptContents))
        {
            return;
        }

        var expectedHostAssemblyName = $"{Path.GetFileNameWithoutExtension(hostProject.ProjectPath)}.dll";
        if (!deployScriptContents.Contains("azure-app-service.zip", StringComparison.Ordinal) ||
            !deployScriptContents.Contains(expectedHostAssemblyName, StringComparison.Ordinal) ||
            !deployScriptContents.Contains(generatedAppId, StringComparison.Ordinal))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated Azure App Service baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, deployScriptPath)} no longer keeps the generated ZIP package and published host defaults aligned with {generatedAppId}.",
                "Restore the generated Azure App Service deploy script so the published host DLL and ZIP package defaults stay aligned with the current app root."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated Azure App Service baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, deployScriptPath)} keeps the generated ZIP package and published host defaults aligned with {generatedAppId}.",
            null));
    }

    private static void EvaluateGeneratedLinuxSystemdBaseline(
        GeneratedHostProject hostProject,
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var serviceFilePath = Path.Combine(generatedAppRootPath, "deploy", "linux", "systemd", $"{generatedAppId}.service");
        if (!File.Exists(serviceFilePath))
        {
            return;
        }

        if (!TryReadGeneratedTextAsset(
                serviceFilePath,
                generatedAppRootPath,
                "Generated Linux systemd baseline",
                "Fix the generated Linux systemd unit before rerunning `cephalon doctor --app-root`.",
                checks,
                out var serviceFileContents))
        {
            return;
        }

        var expectedHostAssemblyName = $"{Path.GetFileNameWithoutExtension(hostProject.ProjectPath)}.dll";
        if (!serviceFileContents.Contains(expectedHostAssemblyName, StringComparison.Ordinal) ||
            !serviceFileContents.Contains($"/opt/{generatedAppId}/current", StringComparison.Ordinal) ||
            !serviceFileContents.Contains($"/etc/cephalon/{generatedAppId}.env", StringComparison.Ordinal))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated Linux systemd baseline",
                $"{ToDisplayRelativePath(generatedAppRootPath, serviceFilePath)} no longer keeps the generated Linux systemd unit aligned with {generatedAppId} and {expectedHostAssemblyName}.",
                "Restore the generated Linux systemd unit so the published host DLL, install root, and environment-file defaults stay aligned with the current app root."));
            return;
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated Linux systemd baseline",
            $"{ToDisplayRelativePath(generatedAppRootPath, serviceFilePath)} keeps the generated Linux systemd unit aligned with {generatedAppId} and {expectedHostAssemblyName}.",
            null));
    }

    private static void EvaluateGeneratedWindowsServiceTeardownBaseline(
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var removeScriptPath = Path.Combine(generatedAppRootPath, "deploy", "windows-service", "remove-service.ps1");
        EvaluateGeneratedScriptBaseline(
            removeScriptPath,
            generatedAppRootPath,
            "Generated Windows Service teardown baseline",
            RequiredGeneratedWindowsServiceRemoveScriptMarkers,
            $"{ToDisplayRelativePath(generatedAppRootPath, removeScriptPath)} keeps the generated Windows Service stop/delete flow explicit for {generatedAppId}.",
            "Restore the generated Windows Service remove script so the service stop/delete flow stays aligned with the current app root.",
            checks);
    }

    private static void EvaluateGeneratedIisTeardownBaseline(
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var removeScriptPath = Path.Combine(generatedAppRootPath, "deploy", "iis", "remove-site.ps1");
        EvaluateGeneratedScriptBaseline(
            removeScriptPath,
            generatedAppRootPath,
            "Generated IIS teardown baseline",
            RequiredGeneratedIisRemoveScriptMarkers,
            $"{ToDisplayRelativePath(generatedAppRootPath, removeScriptPath)} keeps the generated IIS stop/delete flow explicit for {generatedAppId}.",
            "Restore the generated IIS remove script so the site/app-pool teardown flow stays aligned with the current app root.",
            checks);
    }

    private static void EvaluateGeneratedLinuxSystemdEnvironmentBaseline(
        string generatedAppRootPath,
        string generatedAppId,
        ICollection<DoctorCheck> checks)
    {
        var environmentFilePath = Path.Combine(generatedAppRootPath, "deploy", "linux", "systemd", $"{generatedAppId}.env");
        EvaluateGeneratedTextAssetBaseline(
            environmentFilePath,
            generatedAppRootPath,
            "Generated Linux systemd environment baseline",
            RequiredGeneratedLinuxSystemdEnvironmentFileMarkers,
            $"{ToDisplayRelativePath(generatedAppRootPath, environmentFilePath)} keeps the generated Linux systemd environment defaults explicit for {generatedAppId}.",
            "no longer keeps the generated Linux systemd environment baseline explicit for",
            "Restore the generated Linux systemd environment file so the default environment and telemetry override hints stay aligned with the current app root.",
            checks);
    }

    private static void AddGeneratedAppDeploymentModeCheck(
        ICollection<DoctorCheck> checks,
        string generatedAppRootPath,
        string title,
        string propertyName,
        DeploymentModeSupportMode supportMode,
        XDocument projectDocument,
        string projectPath,
        XDocument? publishProfileDocument,
        string publishProfilePath)
    {
        var observation = ResolveMsBuildPropertyObservation(
            propertyName,
            generatedAppRootPath,
            projectDocument,
            projectPath,
            publishProfileDocument,
            publishProfilePath);

        if (observation is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                title,
                $"{propertyName} is not enabled in the generated app bootstrap.",
                null));
            return;
        }

        if (!bool.TryParse(observation.Value, out var enabled))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Warning,
                title,
                $"Could not parse {propertyName}='{observation.Value}' from {observation.SourceDisplayPath}.",
                "Use explicit `true` or `false` values if you want doctor to validate this deployment-mode posture."));
            return;
        }

        if (!enabled)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                title,
                $"{propertyName}=false in {observation.SourceDisplayPath}.",
                null));
            return;
        }

        var claimed = string.Equals(supportMode.Status, "claimed", StringComparison.OrdinalIgnoreCase);
        checks.Add(new DoctorCheck(
            claimed ? DoctorCheckSeverity.Pass : DoctorCheckSeverity.Warning,
            title,
            claimed
                ? $"{propertyName}=true in {observation.SourceDisplayPath}. {supportMode.Summary}"
                : $"{propertyName}=true in {observation.SourceDisplayPath}, but the support contract remains {supportMode.Status}. {supportMode.Summary}",
            claimed
                ? null
                : "Treat this generated app as outside the supported external-adoption baseline until you remove the deployment-mode claim or the support contract changes."));
    }

    private static string[] ExtractTargetFrameworks(XDocument projectDocument)
    {
        return projectDocument
            .Descendants()
            .Where(element =>
                string.Equals(element.Name.LocalName, "TargetFramework", StringComparison.Ordinal) ||
                string.Equals(element.Name.LocalName, "TargetFrameworks", StringComparison.Ordinal))
            .Select(element => element.Value)
            .SelectMany(value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeMsBuildPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Replace('\\', '/').Trim();
    }

    private static GeneratedDeploymentBaseline? ResolveGeneratedDeploymentBaseline(
        string[] targetFrameworks,
        DeploymentModeSupportContract? supportContract)
    {
        if (targetFrameworks.Length == 0)
        {
            return null;
        }

        if (supportContract is null)
        {
            var targetFramework = targetFrameworks[0];
            return BuildGeneratedDeploymentBaseline(
                targetFramework,
                DoctorCheckSeverity.Pass,
                $"for host target framework {targetFramework}.",
                null);
        }

        var stableTargetFramework = supportContract.ShippingBaseline.StableTargetFramework;
        if (targetFrameworks.Any(targetFramework => string.Equals(targetFramework, stableTargetFramework, StringComparison.OrdinalIgnoreCase)))
        {
            return BuildGeneratedDeploymentBaseline(
                stableTargetFramework,
                DoctorCheckSeverity.Pass,
                "for the stable shipping floor.",
                null);
        }

        var readinessLaneTargetFramework = supportContract.ShippingBaseline.ReadinessLaneTargetFramework;
        if (targetFrameworks.Any(targetFramework => string.Equals(targetFramework, readinessLaneTargetFramework, StringComparison.OrdinalIgnoreCase)))
        {
            return BuildGeneratedDeploymentBaseline(
                readinessLaneTargetFramework,
                DoctorCheckSeverity.Warning,
                "for the assessment-only readiness lane.",
                $"Keep `{stableTargetFramework}` for supported external adoption, or treat `{readinessLaneTargetFramework}` as readiness-only until the support contract changes.");
        }

        return null;
    }

    private static GeneratedDeploymentBaseline? BuildGeneratedDeploymentBaseline(
        string targetFramework,
        DoctorCheckSeverity alignedSeverity,
        string alignedDetail,
        string? alignedGuidance)
    {
        var imageTag = TryGetDotNetContainerImageTag(targetFramework);
        return string.IsNullOrWhiteSpace(imageTag)
            ? null
            : new GeneratedDeploymentBaseline(
                targetFramework,
                imageTag,
                alignedSeverity,
                alignedDetail,
                alignedGuidance);
    }

    private static string ResolveGeneratedAppId(
        string? solutionPath,
        string generatedAppRootPath,
        GeneratedHostProject? hostProject = null)
    {
        var solutionStem = Path.GetFileNameWithoutExtension(solutionPath);
        if (!string.IsNullOrWhiteSpace(solutionStem))
        {
            return solutionStem;
        }

        var hostProjectStem = Path.GetFileNameWithoutExtension(hostProject?.ProjectPath);
        if (!string.IsNullOrWhiteSpace(hostProjectStem))
        {
            return hostProjectStem;
        }

        var directoryInfo = new DirectoryInfo(generatedAppRootPath);
        return string.IsNullOrWhiteSpace(directoryInfo.Name)
            ? "CephalonApp"
            : directoryInfo.Name;
    }

    private static string? TryGetDotNetContainerImageTag(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return null;
        }

        var normalized = targetFramework.Trim();
        if (!normalized.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = normalized[3..];
        var tagCharacters = suffix
            .TakeWhile(character => char.IsDigit(character) || character == '.')
            .ToArray();

        return tagCharacters.Length == 0
            ? null
            : new string(tagCharacters);
    }

    private static string? ExtractDotNetDockerImageTag(IEnumerable<string> dockerfileLines, string prefix)
    {
        foreach (var line in dockerfileLines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remainder = trimmed[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(remainder))
            {
                return null;
            }

            var separatorIndex = remainder.IndexOfAny([' ', '\t']);
            return separatorIndex >= 0
                ? remainder[..separatorIndex].Trim()
                : remainder;
        }

        return null;
    }

    private static string BuildGeneratedAzureContainerAppName(string generatedAppId)
    {
        var slug = BuildGeneratedDeploymentSlug(generatedAppId, "cephalon-app");
        if (!char.IsLetter(slug[0]))
        {
            slug = $"c-{slug}";
        }

        return TrimGeneratedDeploymentSlug(slug, 31, requireLeadingAlphaNumeric: false, fallbackValue: "cephalon-app");
    }

    private static string BuildGeneratedKubernetesResourceName(string generatedAppId)
    {
        var slug = BuildGeneratedDeploymentSlug(generatedAppId, "cephalon-app");
        return TrimGeneratedDeploymentSlug(slug, 63, requireLeadingAlphaNumeric: true, fallbackValue: "cephalon-app");
    }

    private static string BuildGeneratedContainerImagePlaceholder(string generatedAppId)
    {
        return $"replace-with-registry/{BuildGeneratedKubernetesResourceName(generatedAppId)}:latest";
    }

    private static string BuildGeneratedDeploymentSlug(string value, string fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackValue;
        }

        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? fallbackValue : slug;
    }

    private static string TrimGeneratedDeploymentSlug(
        string slug,
        int maxLength,
        bool requireLeadingAlphaNumeric,
        string fallbackValue)
    {
        var normalized = slug;
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength].TrimEnd('-');
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallbackValue;
        }

        if (requireLeadingAlphaNumeric && !char.IsLetterOrDigit(normalized[0]))
        {
            normalized = $"app-{normalized}";
            if (normalized.Length > maxLength)
            {
                normalized = normalized[..maxLength].TrimEnd('-');
            }
        }

        if (!char.IsLetterOrDigit(normalized[^1]))
        {
            normalized = normalized.TrimEnd('-');
        }

        return string.IsNullOrWhiteSpace(normalized) ? fallbackValue : normalized;
    }

    private static bool ContainsAllFragments(string contents, params string[] fragments)
    {
        return fragments.All(fragment => contents.Contains(fragment, StringComparison.Ordinal));
    }

    private static bool TryReadGeneratedTextAsset(
        string path,
        string generatedAppRootPath,
        string checkTitle,
        string failureGuidance,
        ICollection<DoctorCheck> checks,
        out string contents)
    {
        try
        {
            contents = File.ReadAllText(path);
            return true;
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                checkTitle,
                $"Could not inspect {ToDisplayRelativePath(generatedAppRootPath, path)}: {exception.Message}",
                failureGuidance));
            contents = string.Empty;
            return false;
        }
    }

    private static bool TryReadGeneratedJsonObjectAsset(
        string path,
        string generatedAppRootPath,
        string checkTitle,
        string failureGuidance,
        ICollection<DoctorCheck> checks,
        out JsonObject rootObject)
    {
        if (!TryReadGeneratedTextAsset(
                path,
                generatedAppRootPath,
                checkTitle,
                failureGuidance,
                checks,
                out var contents))
        {
            rootObject = new JsonObject();
            return false;
        }

        try
        {
            if (JsonNode.Parse(contents) is JsonObject parsedObject)
            {
                rootObject = parsedObject;
                return true;
            }

            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                checkTitle,
                $"{ToDisplayRelativePath(generatedAppRootPath, path)} did not parse as a JSON object.",
                failureGuidance));
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                checkTitle,
                $"Could not parse {ToDisplayRelativePath(generatedAppRootPath, path)}: {exception.Message}",
                failureGuidance));
        }

        rootObject = new JsonObject();
        return false;
    }

    private static string? GetRequiredJsonString(JsonObject node, string propertyName)
    {
        var value = node[propertyName];
        return value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue)
            ? stringValue?.Trim()
            : null;
    }

    private static bool TryGetJsonObject(JsonObject node, string propertyName, out JsonObject value)
    {
        if (node[propertyName] is JsonObject objectValue)
        {
            value = objectValue;
            return true;
        }

        value = new JsonObject();
        return false;
    }

    private static bool TryGetJsonArray(JsonObject node, string propertyName, out JsonArray value)
    {
        if (node[propertyName] is JsonArray arrayValue)
        {
            value = arrayValue;
            return true;
        }

        value = new JsonArray();
        return false;
    }

    private static bool JsonArrayContainsOnlyStrings(JsonArray array)
    {
        return array.All(item => item is JsonValue jsonValue && jsonValue.TryGetValue<string>(out _));
    }

    private static bool JsonArrayContainsStringValue(JsonArray array, string expectedValue)
    {
        return array.Any(item =>
            item is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var stringValue) &&
            string.Equals(stringValue?.Trim(), expectedValue, StringComparison.Ordinal));
    }

    private static bool JsonArrayContainsObjectWithString(JsonArray array, string propertyName, string expectedValue)
    {
        return array.Any(item =>
            item is JsonObject objectValue &&
            string.Equals(GetRequiredJsonString(objectValue, propertyName), expectedValue, StringComparison.Ordinal));
    }

    private static bool TryGetRequiredBoolean(JsonObject node, string propertyName, out bool value)
    {
        var property = node[propertyName];
        if (property is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value))
        {
            return true;
        }

        value = false;
        return false;
    }

    private static MsBuildPropertyObservation? ResolveMsBuildPropertyObservation(
        string propertyName,
        string generatedAppRootPath,
        XDocument projectDocument,
        string projectPath,
        XDocument? publishProfileDocument,
        string publishProfilePath)
    {
        var publishProfileValue = publishProfileDocument is null
            ? null
            : FindMsBuildPropertyValue(publishProfileDocument, propertyName);
        if (!string.IsNullOrWhiteSpace(publishProfileValue))
        {
            return new MsBuildPropertyObservation(
                publishProfileValue,
                ToDisplayRelativePath(generatedAppRootPath, publishProfilePath));
        }

        var projectValue = FindMsBuildPropertyValue(projectDocument, propertyName);
        return string.IsNullOrWhiteSpace(projectValue)
            ? null
            : new MsBuildPropertyObservation(
                projectValue,
                ToDisplayRelativePath(generatedAppRootPath, projectPath));
    }

    private static string? FindMsBuildPropertyValue(XDocument document, string propertyName)
    {
        return document
            .Descendants()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))
            ?.Value
            .Trim();
    }

    private static void EvaluatePackageSource(
        string nuGetConfigPath,
        string generatedAppRootPath,
        ICollection<DoctorCheck> checks)
    {
        try
        {
            var document = XDocument.Load(nuGetConfigPath);
            var cephalonSource = document
                .Descendants("packageSources")
                .Elements("add")
                .FirstOrDefault(element => string.Equals((string?)element.Attribute("key"), "cephalon", StringComparison.OrdinalIgnoreCase));

            if (cephalonSource is null)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Cephalon package source",
                    "`NuGet.config` does not define a `cephalon` package source.",
                    "Add a `cephalon` package source that points at `./.cephalon/packages` or at your shared Cephalon feed before restoring the app."));
                return;
            }

            var sourceValue = ((string?)cephalonSource.Attribute("value"))?.Trim();
            if (string.IsNullOrWhiteSpace(sourceValue))
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Cephalon package source",
                    "`NuGet.config` defines `cephalon`, but its `value` is empty.",
                    "Point the `cephalon` package source at `./.cephalon/packages` or at your shared Cephalon feed before restoring the app."));
                return;
            }

            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Cephalon package source",
                sourceValue,
                null));

            var mapping = document
                .Descendants("packageSourceMapping")
                .Elements("packageSource")
                .FirstOrDefault(element => string.Equals((string?)element.Attribute("key"), "cephalon", StringComparison.OrdinalIgnoreCase));

            var hasCephalonMapping = mapping?
                .Elements("package")
                .Select(element => ((string?)element.Attribute("pattern"))?.Trim())
                .Any(pattern => string.Equals(pattern, "Cephalon*", StringComparison.Ordinal)) == true;

            checks.Add(new DoctorCheck(
                hasCephalonMapping ? DoctorCheckSeverity.Pass : DoctorCheckSeverity.Warning,
                "Cephalon package source mapping",
                hasCephalonMapping
                    ? "Cephalon* packages stay pinned to the `cephalon` source."
                    : "No `Cephalon*` packageSourceMapping entry was found for the `cephalon` source.",
                hasCephalonMapping
                    ? null
                    : "Add `packageSourceMapping` for `Cephalon*` if you want restore to stay deterministic across multiple feeds."));

            if (!TryResolveLocalPackageSource(sourceValue, generatedAppRootPath, out var localFeedPath))
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Cephalon package source reachability",
                    "Remote/shared feed configured. Doctor cannot validate package contents locally.",
                    null));
                return;
            }

            if (!Directory.Exists(localFeedPath))
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Cephalon local package feed",
                    $"Local package source '{localFeedPath}' does not exist.",
                    "Populate the generated `./.cephalon/packages` directory with Cephalon packages or replace the `cephalon` source in `NuGet.config` before restore/build."));
                return;
            }

            var packageFiles = Directory.GetFiles(localFeedPath, "Cephalon*.nupkg", SearchOption.TopDirectoryOnly)
                .Where(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (packageFiles.Length == 0)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Cephalon local package feed",
                    $"No `Cephalon*.nupkg` files were found under '{localFeedPath}'.",
                    "Populate the generated local feed with Cephalon packages or replace the `cephalon` source in `NuGet.config` before restore/build."));
                return;
            }

            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Cephalon local package feed",
                $"{packageFiles.Length} package(s) found under {ToDisplayRelativePath(generatedAppRootPath, localFeedPath)}.",
                null));
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Cephalon package source",
                $"Could not inspect `NuGet.config`: {exception.Message}",
                "Fix or recreate the generated `NuGet.config` before rerunning doctor."));
        }
    }

    private static string[] ExtractCephalonPackageVersions(string packagePropsPath)
    {
        try
        {
            return XDocument.Load(packagePropsPath)
                .Descendants("PackageVersion")
                .Select(element => new
                {
                    Include = ((string?)element.Attribute("Include"))?.Trim(),
                    Version = ((string?)element.Attribute("Version"))?.Trim()
                })
                .Where(package => !string.IsNullOrWhiteSpace(package.Include) &&
                    package.Include.StartsWith("Cephalon.", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(package.Version))
                .Select(package => $"{package.Include} {package.Version}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Take(4)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static string[] ExtractCephalonPackageReferenceVersions(string projectPath)
    {
        try
        {
            return XDocument.Load(projectPath)
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal))
                .Select(element =>
                {
                    var include = ((string?)element.Attribute("Include"))?.Trim();
                    var version = ((string?)element.Attribute("Version"))?.Trim()
                        ?? element.Elements().FirstOrDefault(child => string.Equals(child.Name.LocalName, "Version", StringComparison.Ordinal))?.Value?.Trim();

                    return new
                    {
                        Include = include,
                        Version = version
                    };
                })
                .Where(package =>
                    !string.IsNullOrWhiteSpace(package.Include) &&
                    package.Include.StartsWith("Cephalon.", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(package.Version))
                .Select(package => $"{package.Include} {package.Version}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Take(4)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static bool TryResolveLocalPackageSource(
        string sourceValue,
        string generatedAppRootPath,
        out string resolvedPath)
    {
        if (Uri.TryCreate(sourceValue, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.IsFile)
            {
                resolvedPath = absoluteUri.LocalPath;
                return true;
            }

            resolvedPath = string.Empty;
            return false;
        }

        resolvedPath = Path.GetFullPath(Path.Combine(generatedAppRootPath, sourceValue));
        return true;
    }

    private static string ToDisplayRelativePath(string rootPath, string path)
    {
        var relativePath = Path.GetRelativePath(rootPath, path).Replace('\\', '/');
        return relativePath.StartsWith("..", StringComparison.Ordinal)
            ? relativePath
            : $"./{relativePath}";
    }

    private static string FormatCommandPath(string path)
    {
        return path.Contains(' ')
            ? $"\"{path}\""
            : path;
    }

    private static string? GetTemplatePackCustomHivePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(TemplatePackCustomHiveEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configuredPath)
            ? null
            : configuredPath.Trim();
    }

    private static string FormatCommandArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "\"\"";
        }

        if (!argument.Any(character => char.IsWhiteSpace(character) || character is '"' or '\''))
        {
            return argument;
        }

        return $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static string BuildDotNetCommandText(IEnumerable<string> arguments)
    {
        return $"dotnet {string.Join(' ', arguments.Select(FormatCommandArgument))}";
    }

    private static string[] BuildTemplateListArguments()
    {
        var arguments = new List<string>
        {
            "new",
            "list",
            "cephalon"
        };

        var customHivePath = GetTemplatePackCustomHivePath();
        if (!string.IsNullOrWhiteSpace(customHivePath))
        {
            arguments.Add("--debug:custom-hive");
            arguments.Add(customHivePath);
        }

        return arguments.ToArray();
    }

    private static string BuildTemplateListCommandText()
    {
        return BuildDotNetCommandText(BuildTemplateListArguments());
    }

    private static string BuildTemplateInstallCommandText()
    {
        var arguments = new List<string>
        {
            "new",
            "install",
            TemplatePackPackageId
        };

        var customHivePath = GetTemplatePackCustomHivePath();
        if (!string.IsNullOrWhiteSpace(customHivePath))
        {
            arguments.Add("--debug:custom-hive");
            arguments.Add(customHivePath);
        }

        return BuildDotNetCommandText(arguments);
    }

    private static string BuildTemplateStarterCommandText()
    {
        var arguments = new List<string>
        {
            "new",
            "cephalon-monolith",
            "-n",
            "Acme.Store.TemplateStarter"
        };

        var customHivePath = GetTemplatePackCustomHivePath();
        if (!string.IsNullOrWhiteSpace(customHivePath))
        {
            arguments.Add("--debug:custom-hive");
            arguments.Add(customHivePath);
        }

        return BuildDotNetCommandText(arguments);
    }

    private static async Task<CommandProcessResult?> TryRunDotNetAsync(
        IReadOnlyList<string> arguments,
        string checkTitle,
        string failureGuidance,
        ICollection<DoctorCheck> checks,
        CancellationToken cancellationToken,
        bool treatFailureAsWarning = false,
        bool reportNonZeroExit = true)
    {
        try
        {
            var result = await CommandProcessRunner.RunAsync(
                DotNetExecutable,
                arguments,
                cancellationToken: cancellationToken);

            if (result.ExitCode == 0)
            {
                return result;
            }

            if (reportNonZeroExit)
            {
                checks.Add(new DoctorCheck(
                    treatFailureAsWarning ? DoctorCheckSeverity.Warning : DoctorCheckSeverity.Failure,
                    checkTitle,
                    BuildCommandFailureMessage(arguments, result),
                    failureGuidance));
            }

            return treatFailureAsWarning ? result : null;
        }
        catch (Exception exception)
        {
            checks.Add(new DoctorCheck(
                treatFailureAsWarning ? DoctorCheckSeverity.Warning : DoctorCheckSeverity.Failure,
                checkTitle,
                $"Could not run `dotnet {string.Join(' ', arguments)}`: {exception.Message}",
                failureGuidance));
            return null;
        }
    }

    private static void AddRuntimeCheck(ICollection<DoctorCheck> checks, string runtimeOutput, string runtimeName)
    {
        var matchingRuntimeVersion = ParseRuntimeVersions(runtimeOutput, runtimeName)
            .Where(version => version.Major == RequiredMajorVersion)
            .OrderByDescending(version => version)
            .FirstOrDefault();

        if (matchingRuntimeVersion is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                runtimeName,
                $"No {RequiredMajorVersion}.x runtime was found in `dotnet --list-runtimes`.",
                "Install the .NET 10 SDK or the matching ASP.NET Core hosting/runtime bundle."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                runtimeName,
                matchingRuntimeVersion.ToString(),
                null));
        }
    }

    private static Version[] ParseSdkVersions(string output)
    {
        return output
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(TryParseVersionToken)
            .OfType<Version>()
            .ToArray();
    }

    private static Version[] ParseRuntimeVersions(string output, string runtimeName)
    {
        return output
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith($"{runtimeName} ", StringComparison.Ordinal))
            .Select(line => line.Substring(runtimeName.Length).TrimStart())
            .Select(TryParseVersionToken)
            .OfType<Version>()
            .ToArray();
    }

    private static Version? TryParseVersionToken(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var firstLine = output
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return null;
        }

        var token = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return Version.TryParse(token, out var version)
            ? version
            : null;
    }

    private static string BuildCommandFailureMessage(IReadOnlyList<string> arguments, CommandProcessResult result)
    {
        var combinedOutput = string.Join(
            " ",
            new[] { result.Output.Trim(), result.Error.Trim() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(combinedOutput)
            ? $"`dotnet {string.Join(' ', arguments)}` exited with code {result.ExitCode}."
            : $"`dotnet {string.Join(' ', arguments)}` exited with code {result.ExitCode}: {combinedOutput}";
    }

    private static string GetSeverityLabel(DoctorCheckSeverity severity)
    {
        return severity switch
        {
            DoctorCheckSeverity.Pass => "[ok]",
            DoctorCheckSeverity.Warning => "[warn]",
            _ => "[error]"
        };
    }

    private enum DoctorCheckSeverity
    {
        Pass,
        Warning,
        Failure
    }

    private sealed record DoctorCheck(
        DoctorCheckSeverity Severity,
        string Title,
        string Detail,
        string? Guidance);

    private sealed record DoctorEvaluation(
        IReadOnlyList<DoctorCheck> Checks,
        bool TemplatePackInstalled,
        GeneratedAppDoctorEvaluation? GeneratedApp)
    {
        internal bool HasFailures => Checks.Any(check => check.Severity == DoctorCheckSeverity.Failure);

        internal int FailureCount => Checks.Count(check => check.Severity == DoctorCheckSeverity.Failure);
    }

    private enum GeneratedAppLayoutKind
    {
        Unknown,
        SolutionWithSrcHost,
        ProjectRootStarter
    }

    private sealed record GeneratedAppDoctorEvaluation(
        string ResolvedAppRootPath,
        string? SolutionPath,
        string? HostProjectPath)
    {
        internal string? SolutionRelativePath => SolutionPath is null
            ? null
            : ToDisplayRelativePath(ResolvedAppRootPath, SolutionPath);

        internal string? HostProjectRelativePath => HostProjectPath is null
            ? null
            : ToDisplayRelativePath(ResolvedAppRootPath, HostProjectPath);
    }

    private sealed record GeneratedHostProject(
        string DirectoryPath,
        string ProjectPath,
        string ProgramPath,
        string AppSettingsPath,
        string PublishProfilePath);

    private sealed record GeneratedTestProject(
        string DirectoryPath,
        string ProjectPath,
        string CompositionSmokeTestPath,
        string FeaturesDirectoryPath);

    private sealed record GeneratedDeploymentBaseline(
        string TargetFramework,
        string ExpectedImageTag,
        DoctorCheckSeverity AlignedSeverity,
        string AlignedDetail,
        string? AlignedGuidance);

    private sealed record MsBuildPropertyObservation(
        string Value,
        string SourceDisplayPath);
}
