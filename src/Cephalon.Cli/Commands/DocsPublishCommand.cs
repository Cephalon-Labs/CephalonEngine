using Cephalon.Cli.Console;
using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Implements the <c>cephalon docs publish</c> command.
/// </summary>
internal static class DocsPublishCommand
{
    private const string DefaultTargetFramework = "net10.0";
    private const string DefaultConfiguration = "Debug";
    private const string DefaultBrowserDocument = "browse.html";

    /// <summary>
    /// Executes the docs publish command with the supplied options.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="console">The console abstraction used for user-facing output.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunAsync(
        DocsPublishOptions options,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var request = new ReferenceDocsRequest(
            rootPath: options.RootPath,
            outputPath: options.OutputPath,
            configuration: options.Configuration,
            targetFramework: options.TargetFramework,
            assemblies: options.Assemblies);
        var rendered = ReferenceDocsGenerator.Generate(request);
        await ReferenceDocsWriter.WriteAsync(rendered, overwrite: !options.NoOverwrite, cancellationToken);

        await console.WriteOutputAsync(
            $"Published {rendered.Files.Count} reference doc files to '{options.OutputPath}'.",
            cancellationToken);
        await console.WriteOutputAsync(
            options.Assemblies.Count == 0
                ? "Assemblies: default catalog"
                : $"Assemblies: {string.Join(", ", options.Assemblies)}",
            cancellationToken);

        DocsEnableHostingCommand.ResolvedReferenceDocsSettings? effectiveHostingSettings = null;

        if (options.EnableHosting)
        {
            var appSettingsDirectory = Path.GetDirectoryName(options.AppSettingsPath!)
                ?? Directory.GetCurrentDirectory();
            var hostingOptions = new DocsEnableHostingOptions
            {
                RootPath = options.RootPath,
                AppSettingsPath = options.AppSettingsPath!,
                DirectoryPath = options.HostingDirectoryPath
                    ?? Path.GetRelativePath(appSettingsDirectory, options.OutputPath),
                RoutePrefix = options.HostingRoutePrefix,
                DefaultDocument = options.HostingDefaultDocument
            };

            effectiveHostingSettings = await DocsEnableHostingCommand.ApplyAsync(hostingOptions, cancellationToken);

            await console.WriteOutputAsync(
                $"Enabled hosted reference docs in '{options.AppSettingsPath}'.",
                cancellationToken);
            await console.WriteOutputAsync(
                $"ReferenceDocs => RoutePrefix='{effectiveHostingSettings.RoutePrefix}', DirectoryPath='{effectiveHostingSettings.DirectoryPath}', DefaultDocument='{effectiveHostingSettings.DefaultDocument}'",
                cancellationToken);
        }

        if (options.OpenOutput)
        {
            var launchTarget = options.HostUrl is null
                ? BuildLocalReferenceDocsUri(options.OutputPath)
                : BuildHostedReferenceDocsUri(options.HostUrl, effectiveHostingSettings!);

            await DocumentationLauncher.OpenAsync(launchTarget, cancellationToken);
            await console.WriteOutputAsync($"Opened reference docs: {launchTarget}", cancellationToken);
        }

        if (options.ValidateHosting)
        {
            var validationResult = await DocsValidateHostingCommand.ValidateAsync(
                new DocsValidateHostingOptions
                {
                    AppSettingsPath = options.AppSettingsPath!,
                    HostUrl = options.HostUrl
                },
                cancellationToken);

            await DocsValidateHostingCommand.WriteSuccessAsync(
                validationResult,
                options.AppSettingsPath!,
                console,
                cancellationToken);
        }

        return 0;
    }

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="DocsPublishOptions" /> instance.
    /// </summary>
    /// <param name="args">The raw arguments that follow the <c>docs publish</c> command.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded.</param>
    /// <param name="error">When this method returns, contains the parse error if parsing failed.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    internal static bool TryParse(
        string[] args,
        out DocsPublishOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        var rootPath = Directory.GetCurrentDirectory();
        var configuration = DefaultConfiguration;
        var targetFramework = DefaultTargetFramework;
        string? outputPath = null;
        var assemblies = new List<string>();
        var noOverwrite = false;
        var enableHosting = false;
        string? appSettingsPath = null;
        string? directoryPath = null;
        string? routePrefix = null;
        string? defaultDocument = null;
        var openOutput = false;
        string? hostUrl = null;
        var validateHosting = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--root":
                    if (!TryReadValue(args, ref index, out var rootValue, out error))
                    {
                        return false;
                    }

                    rootPath = Path.GetFullPath(rootValue!);
                    break;
                case "--output":
                    if (!TryReadValue(args, ref index, out var outputValue, out error))
                    {
                        return false;
                    }

                    outputPath = Path.GetFullPath(outputValue!);
                    break;
                case "--configuration":
                    if (!TryReadValue(args, ref index, out var configurationValue, out error))
                    {
                        return false;
                    }

                    configuration = configurationValue!;
                    break;
                case "--target-framework":
                    if (!TryReadValue(args, ref index, out var frameworkValue, out error))
                    {
                        return false;
                    }

                    targetFramework = frameworkValue!;
                    break;
                case "--assembly":
                    if (!TryReadValue(args, ref index, out var assemblyValue, out error))
                    {
                        return false;
                    }

                    assemblies.Add(assemblyValue!);
                    break;
                case "--no-overwrite":
                    noOverwrite = true;
                    break;
                case "--open":
                    openOutput = true;
                    break;
                case "--validate-hosting":
                    validateHosting = true;
                    break;
                case "--host-url":
                    if (!TryReadValue(args, ref index, out var hostUrlValue, out error))
                    {
                        return false;
                    }

                    hostUrl = hostUrlValue!;
                    break;
                case "--enable-hosting":
                    enableHosting = true;
                    break;
                case "--appsettings":
                    if (!TryReadValue(args, ref index, out var appSettingsValue, out error))
                    {
                        return false;
                    }

                    appSettingsPath = Path.GetFullPath(appSettingsValue!);
                    break;
                case "--directory":
                    if (!TryReadValue(args, ref index, out var directoryValue, out error))
                    {
                        return false;
                    }

                    directoryPath = directoryValue!;
                    break;
                case "--route-prefix":
                    if (!TryReadValue(args, ref index, out var routePrefixValue, out error))
                    {
                        return false;
                    }

                    routePrefix = routePrefixValue!;
                    break;
                case "--default-document":
                    if (!TryReadValue(args, ref index, out var defaultDocumentValue, out error))
                    {
                        return false;
                    }

                    defaultDocument = defaultDocumentValue!;
                    break;
                default:
                    error = $"Unknown option '{argument}'.";
                    return false;
            }
        }

        if (enableHosting && string.IsNullOrWhiteSpace(appSettingsPath))
        {
            error = "Option '--enable-hosting' requires '--appsettings <path>'.";
            return false;
        }

        if (!enableHosting &&
            (!string.IsNullOrWhiteSpace(appSettingsPath) ||
             !string.IsNullOrWhiteSpace(directoryPath) ||
             !string.IsNullOrWhiteSpace(routePrefix) ||
             !string.IsNullOrWhiteSpace(defaultDocument)))
        {
            error = "Hosting options require '--enable-hosting'.";
            return false;
        }

        if (!openOutput && !validateHosting && !string.IsNullOrWhiteSpace(hostUrl))
        {
            error = "Option '--host-url' requires '--open' or '--validate-hosting'.";
            return false;
        }

        if (!enableHosting && !validateHosting && !string.IsNullOrWhiteSpace(hostUrl))
        {
            error = "Option '--host-url' requires '--enable-hosting' or '--validate-hosting'.";
            return false;
        }

        if (validateHosting && string.IsNullOrWhiteSpace(appSettingsPath))
        {
            error = "Option '--validate-hosting' requires '--appsettings <path>'.";
            return false;
        }

        options = new DocsPublishOptions
        {
            RootPath = rootPath,
            OutputPath = outputPath ?? Path.Combine(rootPath, "docs", "reference"),
            Configuration = configuration,
            TargetFramework = targetFramework,
            Assemblies = assemblies.ToArray(),
            NoOverwrite = noOverwrite,
            EnableHosting = enableHosting,
            AppSettingsPath = appSettingsPath,
            HostingDirectoryPath = directoryPath,
            HostingRoutePrefix = routePrefix,
            HostingDefaultDocument = defaultDocument,
            OpenOutput = openOutput,
            HostUrl = hostUrl,
            ValidateHosting = validateHosting
        };
        return true;
    }

    private static Uri BuildLocalReferenceDocsUri(string outputPath)
    {
        return new Uri(Path.Combine(outputPath, DefaultBrowserDocument));
    }

    private static Uri BuildHostedReferenceDocsUri(
        string hostUrl,
        DocsEnableHostingCommand.ResolvedReferenceDocsSettings effectiveHostingSettings)
    {
        if (!Uri.TryCreate(hostUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException(
                $"Host URL '{hostUrl}' must be an absolute URL.");
        }

        var routePrefix = effectiveHostingSettings.RoutePrefix.Trim('/');
        var defaultDocument = effectiveHostingSettings.DefaultDocument.TrimStart('/');
        var relativePath = string.IsNullOrWhiteSpace(routePrefix)
            ? defaultDocument
            : $"{routePrefix}/{defaultDocument}";

        return new Uri(baseUri, relativePath);
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
