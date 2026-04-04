using Cephalon.Cli.Console;

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

        var evaluation = await EvaluateAsync(cancellationToken);

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
                $"Cephalon doctor found {evaluation.FailureCount} required issue(s). Install or select the missing prerequisites and rerun `cephalon doctor`.",
                cancellationToken);
            await console.WriteErrorAsync(
                "Required baseline: current dotnet SDK selection 10.x, an installed 10.x SDK family, Microsoft.NETCore.App 10.x, and Microsoft.AspNetCore.App 10.x.",
                cancellationToken);
            return 1;
        }

        var templateSummary = evaluation.TemplatePackInstalled
            ? "The optional template-pack path is also ready."
            : $"The optional template-pack path still needs `dotnet new install {TemplatePackPackageId}` if you want `dotnet new` starters in addition to `cephalon new`.";

        await console.WriteOutputAsync(
            $"Environment is ready for Cephalon CLI scaffolding. {templateSummary}",
            cancellationToken);
        await console.WriteOutputAsync("Next steps:", cancellationToken);
        await console.WriteOutputAsync("  cephalon new Acme.Store --output ./Acme.Store", cancellationToken);
        await console.WriteOutputAsync("  dotnet run --project ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj", cancellationToken);
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

        if (args.Length > 0)
        {
            error = $"Unknown option '{args[0]}'.";
            return false;
        }

        options = new DoctorOptions();
        return true;
    }

    private static async Task<DoctorEvaluation> EvaluateAsync(CancellationToken cancellationToken)
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
            return new DoctorEvaluation(checks, false);
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
            return new DoctorEvaluation(checks, false);
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
            return new DoctorEvaluation(checks, false);
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

        return new DoctorEvaluation(checks, templatePackInstalled);
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
        bool TemplatePackInstalled)
    {
        internal bool HasFailures => Checks.Any(check => check.Severity == DoctorCheckSeverity.Failure);

        internal int FailureCount => Checks.Count(check => check.Severity == DoctorCheckSeverity.Failure);
    }
}
