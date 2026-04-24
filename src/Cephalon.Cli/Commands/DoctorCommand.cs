using Cephalon.Cli.Console;
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

    private static readonly string[] ExpectedTemplateShortNames =
    [
        "cephalon-monolith",
        "cephalon-slice",
        "cephalon-microservice",
        "cephalon-module",
        "cephalon-rest-module"
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
                evaluation.GeneratedApp is null
                    ? $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Install or select the missing prerequisites and rerun `cephalon doctor`."
                    : $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Fix the missing prerequisites or generated-app bootstrap blockers and rerun `cephalon doctor --app-root {FormatCommandPath(evaluation.GeneratedApp.ResolvedAppRootPath)}`.",
                cancellationToken);
            await console.WriteErrorAsync(
                evaluation.GeneratedApp is null
                    ? "Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, and Microsoft.AspNetCore.App 10.x."
                    : "Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, Microsoft.AspNetCore.App 10.x, and a generated app root with a usable Cephalon package source plus host bootstrap assets.",
                cancellationToken);
            return 1;
        }

        var templateSummary = evaluation.TemplatePackInstalled
            ? "The optional template-pack path is also ready."
            : $"The optional template-pack path still needs `dotnet new install {TemplatePackPackageId}` if you want `dotnet new` starters in addition to `cephalon new`.";

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

            await console.WriteOutputAsync(
                $"  dotnet run --project {FormatCommandPath(evaluation.GeneratedApp.HostProjectRelativePath!)}",
                cancellationToken);
        }

        await console.WriteOutputAsync("  Browse /engine, /engine/manifest, /engine/snapshot, /health/ready, and /scalar once the host starts", cancellationToken);
        await console.WriteOutputAsync("Optional template-pack path:", cancellationToken);

        if (!evaluation.TemplatePackInstalled)
        {
            await console.WriteOutputAsync($"  dotnet new install {TemplatePackPackageId}", cancellationToken);
            await console.WriteOutputAsync("  dotnet new list cephalon", cancellationToken);
        }

        await console.WriteOutputAsync("  dotnet new cephalon-monolith -n Acme.Store.TemplateStarter", cancellationToken);
        return 0;
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
                default:
                    error = $"Unknown option '{args[index]}'.";
                    return false;
            }
        }

        options = new DoctorOptions
        {
            AppRootPath = appRootPath
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
        var templateListResult = await TryRunDotNetAsync(
            ["new", "list", "cephalon"],
            "Cephalon template pack",
            $"Install with `dotnet new install {TemplatePackPackageId}` if you want `dotnet new` starters.",
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
                    "No Cephalon templates were found by `dotnet new list cephalon`.",
                    $"Install with `dotnet new install {TemplatePackPackageId}` and verify with `dotnet new list cephalon`."));
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

        if (solutionPath is null)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated app solution",
                "No `.slnx` file was found at the generated app root.",
                "Use the root produced by `cephalon new` or restore the solution file before rerunning doctor."));
        }
        else
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Pass,
                "Generated app solution",
                ToDisplayRelativePath(resolvedAppRootPath, solutionPath),
                null));
        }

        var packagePropsPath = Path.Combine(resolvedAppRootPath, "Directory.Packages.props");
        if (!File.Exists(packagePropsPath))
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated package baseline",
                "Missing `Directory.Packages.props` at the generated app root.",
                "Restore the generated package baseline or regenerate the app before continuing."));
        }
        else
        {
            var cephalonPackageVersions = ExtractCephalonPackageVersions(packagePropsPath);
            if (cephalonPackageVersions.Length == 0)
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Failure,
                    "Generated package baseline",
                    "`Directory.Packages.props` does not define any `Cephalon*` package versions.",
                    "Keep the generated package baseline intact or restore the Cephalon package references before rerunning doctor."));
            }
            else
            {
                checks.Add(new DoctorCheck(
                    DoctorCheckSeverity.Pass,
                    "Generated package baseline",
                    string.Join(", ", cephalonPackageVersions),
                    null));
            }
        }

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

        var hostProjectDirectories = Directory.Exists(Path.Combine(resolvedAppRootPath, "src"))
            ? Directory.GetDirectories(Path.Combine(resolvedAppRootPath, "src"), "*", SearchOption.TopDirectoryOnly)
            : [];

        var hostProjects = hostProjectDirectories
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
                Path.Combine(candidate.Directory, "appsettings.json"),
                Path.Combine(candidate.Directory, "Properties", "PublishProfiles", "CephalonFolder.pubxml")))
            .Where(candidate => File.Exists(candidate.AppSettingsPath))
            .OrderBy(candidate => candidate.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (hostProjects.Length == 0)
        {
            checks.Add(new DoctorCheck(
                DoctorCheckSeverity.Failure,
                "Generated host project",
                "No generated host project with `appsettings.json` was found under `src/`.",
                "Use the root emitted by `cephalon new` or restore the generated host project before rerunning doctor."));
            return new GeneratedAppDoctorEvaluation(resolvedAppRootPath, solutionPath, null);
        }

        checks.Add(new DoctorCheck(
            DoctorCheckSeverity.Pass,
            "Generated host project",
            string.Join(", ", hostProjects.Select(project => ToDisplayRelativePath(resolvedAppRootPath, project.ProjectPath))),
            null));

        var selectedHostProject = hostProjects[0];
        EvaluateGeneratedAppSupportContract(selectedHostProject, resolvedAppRootPath, supportContract, checks);

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
        string AppSettingsPath,
        string PublishProfilePath);

    private sealed record MsBuildPropertyObservation(
        string Value,
        string SourceDisplayPath);
}
