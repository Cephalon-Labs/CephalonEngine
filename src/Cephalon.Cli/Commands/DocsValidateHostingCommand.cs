using System.Text.Json.Nodes;
using Cephalon.Cli.Console;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Implements the <c>cephalon docs validate-hosting</c> command.
/// </summary>
internal static class DocsValidateHostingCommand
{
    /// <summary>
    /// Executes the docs validate-hosting command with the supplied options.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="console">The console abstraction used for user-facing output.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunAsync(
        DocsValidateHostingOptions options,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var result = await ValidateAsync(options, cancellationToken);

        await WriteSuccessAsync(result, options.AppSettingsPath, console, cancellationToken);

        return 0;
    }

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="DocsValidateHostingOptions" /> instance.
    /// </summary>
    /// <param name="args">The raw arguments that follow the <c>docs validate-hosting</c> command.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded.</param>
    /// <param name="error">When this method returns, contains the parse error if parsing failed.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    internal static bool TryParse(
        string[] args,
        out DocsValidateHostingOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        string? appSettingsPath = null;
        string? hostUrl = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--appsettings":
                    if (!TryReadValue(args, ref index, out var appSettingsValue, out error))
                    {
                        return false;
                    }

                    appSettingsPath = Path.GetFullPath(appSettingsValue!);
                    break;
                case "--host-url":
                    if (!TryReadValue(args, ref index, out var hostUrlValue, out error))
                    {
                        return false;
                    }

                    hostUrl = hostUrlValue!;
                    break;
                default:
                    error = $"Unknown option '{argument}'.";
                    return false;
            }
        }

        options = new DocsValidateHostingOptions
        {
            AppSettingsPath = appSettingsPath ?? Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
            HostUrl = hostUrl
        };
        return true;
    }

    internal static async Task<ValidationResult> ValidateAsync(
        DocsValidateHostingOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(options.AppSettingsPath))
        {
            throw new FileNotFoundException(
                $"Could not find appsettings file '{options.AppSettingsPath}'.",
                options.AppSettingsPath);
        }

        var rootObject = await ReadRootObjectAsync(options.AppSettingsPath, cancellationToken);
        var referenceDocs = rootObject["ReferenceDocs"] as JsonObject;
        if (referenceDocs is null)
        {
            throw new InvalidOperationException(
                $"The appsettings file '{options.AppSettingsPath}' does not contain a 'ReferenceDocs' section.");
        }

        var enabled = referenceDocs["Enabled"]?.GetValue<bool>() ?? false;
        if (!enabled)
        {
            throw new InvalidOperationException(
                $"ReferenceDocs hosting is disabled in '{options.AppSettingsPath}'.");
        }

        var routePrefix = NormalizeRoutePrefix(
            referenceDocs["RoutePrefix"]?.GetValue<string>() ?? DocsEnableHostingCommand.DefaultRoutePrefix);
        var defaultDocument = NormalizeDefaultDocument(
            referenceDocs["DefaultDocument"]?.GetValue<string>() ?? DocsEnableHostingCommand.DefaultDocument);
        var appSettingsDirectory = Path.GetDirectoryName(options.AppSettingsPath)
            ?? Directory.GetCurrentDirectory();
        var configuredDirectoryPath = referenceDocs["DirectoryPath"]?.GetValue<string>();
        var directoryPath = string.IsNullOrWhiteSpace(configuredDirectoryPath)
            ? Path.Combine("docs", "reference")
            : configuredDirectoryPath.Trim();
        var resolvedDirectoryPath = Path.IsPathRooted(directoryPath)
            ? Path.GetFullPath(directoryPath)
            : Path.GetFullPath(Path.Combine(appSettingsDirectory, directoryPath));

        if (!Directory.Exists(resolvedDirectoryPath))
        {
            throw new DirectoryNotFoundException(
                $"ReferenceDocs directory '{resolvedDirectoryPath}' does not exist.");
        }

        var resolvedDefaultDocumentPath = Path.GetFullPath(Path.Combine(resolvedDirectoryPath, defaultDocument));
        if (!File.Exists(resolvedDefaultDocumentPath))
        {
            throw new FileNotFoundException(
                $"ReferenceDocs default document '{resolvedDefaultDocumentPath}' does not exist.",
                resolvedDefaultDocumentPath);
        }

        Uri? hostedBrowserUri = null;
        Uri? hostedManifestUri = null;
        Uri? engineSurfaceUri = null;

        if (!string.IsNullOrWhiteSpace(options.HostUrl))
        {
            if (!Uri.TryCreate(options.HostUrl, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException(
                    $"Host URL '{options.HostUrl}' must be an absolute URL.");
            }

            hostedBrowserUri = new Uri(baseUri, $"{routePrefix.Trim('/')}/{defaultDocument.TrimStart('/')}");
            hostedManifestUri = new Uri(baseUri, $"{routePrefix.Trim('/')}/reference-manifest.json");
            engineSurfaceUri = new Uri(baseUri, "engine/reference-docs");
        }

        return new ValidationResult(
            RoutePrefix: routePrefix,
            DirectoryPath: directoryPath,
            DefaultDocument: defaultDocument,
            ResolvedDirectoryPath: resolvedDirectoryPath,
            ResolvedDefaultDocumentPath: resolvedDefaultDocumentPath,
            HostedBrowserUri: hostedBrowserUri,
            HostedManifestUri: hostedManifestUri,
            EngineSurfaceUri: engineSurfaceUri);
    }

    internal static async Task WriteSuccessAsync(
        ValidationResult result,
        string appSettingsPath,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(appSettingsPath);
        ArgumentNullException.ThrowIfNull(console);

        await console.WriteOutputAsync(
            $"ReferenceDocs hosting is ready in '{appSettingsPath}'.",
            cancellationToken);
        await console.WriteOutputAsync(
            $"RoutePrefix='{result.RoutePrefix}', DirectoryPath='{result.DirectoryPath}', DefaultDocument='{result.DefaultDocument}'",
            cancellationToken);
        await console.WriteOutputAsync(
            $"Resolved docs directory: {result.ResolvedDirectoryPath}",
            cancellationToken);
        await console.WriteOutputAsync(
            $"Resolved default document: {result.ResolvedDefaultDocumentPath}",
            cancellationToken);

        if (result.HostedBrowserUri is not null)
        {
            await console.WriteOutputAsync(
                $"Hosted browser URL: {result.HostedBrowserUri}",
                cancellationToken);
            await console.WriteOutputAsync(
                $"Hosted manifest URL: {result.HostedManifestUri}",
                cancellationToken);
            await console.WriteOutputAsync(
                $"Engine reference-docs URL: {result.EngineSurfaceUri}",
                cancellationToken);
        }
    }

    private static async Task<JsonObject> ReadRootObjectAsync(
        string appSettingsPath,
        CancellationToken cancellationToken)
    {
        var parsed = JsonNode.Parse(await File.ReadAllTextAsync(appSettingsPath, cancellationToken));
        return parsed as JsonObject
               ?? throw new InvalidOperationException(
                   $"The appsettings file '{appSettingsPath}' must contain a top-level JSON object.");
    }

    private static string NormalizeRoutePrefix(string routePrefix)
    {
        var candidate = routePrefix.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return DocsEnableHostingCommand.DefaultRoutePrefix;
        }

        candidate = "/" + candidate.Trim('/');
        if (candidate == "/")
        {
            throw new InvalidOperationException("ReferenceDocs route prefix must resolve to a non-root path.");
        }

        return candidate;
    }

    private static string NormalizeDefaultDocument(string defaultDocument)
    {
        var candidate = defaultDocument.Trim();
        return string.IsNullOrWhiteSpace(candidate)
            ? DocsEnableHostingCommand.DefaultDocument
            : candidate;
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

    /// <summary>
    /// Represents the validated hosted-reference-doc settings for an app host.
    /// </summary>
    /// <param name="RoutePrefix">The normalized route prefix from configuration.</param>
    /// <param name="DirectoryPath">The configured directory path value.</param>
    /// <param name="DefaultDocument">The configured default document value.</param>
    /// <param name="ResolvedDirectoryPath">The absolute documentation directory resolved from configuration.</param>
    /// <param name="ResolvedDefaultDocumentPath">The absolute default-document path resolved from configuration.</param>
    /// <param name="HostedBrowserUri">The hosted browser URL when a host base URL was supplied.</param>
    /// <param name="HostedManifestUri">The hosted manifest URL when a host base URL was supplied.</param>
    /// <param name="EngineSurfaceUri">The engine reference-doc introspection URL when a host base URL was supplied.</param>
    internal sealed record ValidationResult(
        string RoutePrefix,
        string DirectoryPath,
        string DefaultDocument,
        string ResolvedDirectoryPath,
        string ResolvedDefaultDocumentPath,
        Uri? HostedBrowserUri,
        Uri? HostedManifestUri,
        Uri? EngineSurfaceUri);
}
