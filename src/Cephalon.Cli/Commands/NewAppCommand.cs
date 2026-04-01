using Cephalon.Cli.Console;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Configuration;
using Cephalon.Scaffolding.Generation;
using Cephalon.Scaffolding.IO;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Implements the <c>cephalon new</c> command.
/// </summary>
internal static class NewAppCommand
{
    private const string DefaultBlueprint = "ModularMonolith";
    private const string DefaultModule = "Platform";
    private const string DefaultFeature = "Overview";
    private const string DefaultTransport = "RestApi";
    private const string DefaultPackageVersion = "0.1.0-preview";
    private const string DefaultTargetFramework = "net10.0";

    /// <summary>
    /// Executes the <c>new</c> command with the supplied options.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="console">The console abstraction used for user-facing output.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunAsync(
        NewAppOptions options,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var settings = new EngineSettings(
            blueprint: options.Blueprint,
            patterns: options.Patterns,
            transports: options.Transports,
            technologies: options.Technologies);
        var appProfile = AppProfileFactory.Create(settings);
        var request = new ScaffoldRequest(
            appName: options.AppName,
            modules: options.Modules,
            features: options.Features,
            targetFramework: options.TargetFramework,
            cephalonPackageVersion: options.PackageVersion);
        var rendered = ScaffoldGenerator.Generate(appProfile, request);

        await FileSystemScaffoldWriter.WriteAsync(options.OutputPath, rendered, options.Force, cancellationToken);

        await console.WriteOutputAsync(
            $"Generated '{options.AppName}' at '{options.OutputPath}' using blueprint '{appProfile.BlueprintDisplayName}'.",
            cancellationToken);
        await console.WriteOutputAsync(
            $"Projects: {rendered.Projects.Count}, files: {rendered.Files.Count}, modules: {string.Join(", ", options.Modules)}",
            cancellationToken);

        return 0;
    }

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="NewAppOptions" /> instance.
    /// </summary>
    /// <param name="args">The raw arguments that follow the <c>new</c> command verb.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded.</param>
    /// <param name="error">When this method returns, contains the parse error if parsing failed.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    internal static bool TryParse(
        string[] args,
        out NewAppOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            error = "The 'new' command requires an <AppName>.";
            return false;
        }

        var appName = args[0].Trim();
        var blueprint = DefaultBlueprint;
        var outputPath = Path.GetFullPath(appName);
        var packageVersion = DefaultPackageVersion;
        var targetFramework = DefaultTargetFramework;
        var modules = new List<string>();
        var features = new List<string>();
        var patterns = new List<string>();
        var technologies = new List<string>();
        var transports = new List<string>();
        var force = false;

        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--blueprint":
                    if (!TryReadValue(args, ref index, out var blueprintValue, out error))
                    {
                        return false;
                    }

                    blueprint = blueprintValue!;
                    break;
                case "--module":
                    if (!TryReadValue(args, ref index, out var moduleValue, out error))
                    {
                        return false;
                    }

                    modules.Add(moduleValue!);
                    break;
                case "--feature":
                    if (!TryReadValue(args, ref index, out var featureValue, out error))
                    {
                        return false;
                    }

                    features.Add(featureValue!);
                    break;
                case "--pattern":
                    if (!TryReadValue(args, ref index, out var patternValue, out error))
                    {
                        return false;
                    }

                    patterns.Add(patternValue!);
                    break;
                case "--transport":
                    if (!TryReadValue(args, ref index, out var transportValue, out error))
                    {
                        return false;
                    }

                    transports.Add(transportValue!);
                    break;
                case "--technology":
                    if (!TryReadValue(args, ref index, out var technologyValue, out error))
                    {
                        return false;
                    }

                    technologies.Add(technologyValue!);
                    break;
                case "--output":
                    if (!TryReadValue(args, ref index, out var outputValue, out error))
                    {
                        return false;
                    }

                    outputPath = Path.GetFullPath(outputValue!);
                    break;
                case "--package-version":
                    if (!TryReadValue(args, ref index, out var packageValue, out error))
                    {
                        return false;
                    }

                    packageVersion = packageValue!;
                    break;
                case "--target-framework":
                    if (!TryReadValue(args, ref index, out var frameworkValue, out error))
                    {
                        return false;
                    }

                    targetFramework = frameworkValue!;
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    error = $"Unknown option '{argument}'.";
                    return false;
            }
        }

        if (modules.Count == 0)
        {
            modules.Add(DefaultModule);
        }

        if (features.Count == 0)
        {
            features.Add(DefaultFeature);
        }

        if (transports.Count == 0)
        {
            transports.Add(DefaultTransport);
        }

        options = new NewAppOptions
        {
            AppName = appName,
            Blueprint = blueprint,
            OutputPath = outputPath,
            PackageVersion = packageVersion,
            TargetFramework = targetFramework,
            Modules = modules.ToArray(),
            Features = features.ToArray(),
            Patterns = patterns.ToArray(),
            Technologies = technologies.ToArray(),
            Transports = transports.ToArray(),
            Force = force
        };

        return true;
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        out string? value,
        out string? error)
    {
        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            value = null;
            error = $"Option '{args[index]}' requires a value.";
            return false;
        }

        index++;
        value = args[index].Trim();
        error = null;
        return true;
    }
}
