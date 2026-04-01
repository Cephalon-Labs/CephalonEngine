using System.Text.Json;
using System.Text.Json.Nodes;
using Cephalon.Cli.Console;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Implements the <c>cephalon docs enable-hosting</c> command.
/// </summary>
internal static class DocsEnableHostingCommand
{
    internal const string DefaultRoutePrefix = "/reference";
    internal const string DefaultDocument = "browse.html";

    /// <summary>
    /// Executes the docs enable-hosting command with the supplied options.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="console">The console abstraction used for user-facing output.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunAsync(
        DocsEnableHostingOptions options,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var effectiveSettings = await ApplyAsync(options, cancellationToken);

        await console.WriteOutputAsync(
            $"Enabled hosted reference docs in '{options.AppSettingsPath}'.",
            cancellationToken);
        await console.WriteOutputAsync(
            $"ReferenceDocs => RoutePrefix='{effectiveSettings.RoutePrefix}', DirectoryPath='{effectiveSettings.DirectoryPath}', DefaultDocument='{effectiveSettings.DefaultDocument}'",
            cancellationToken);

        return 0;
    }

    /// <summary>
    /// Applies the requested hosted reference-doc settings to the target appsettings file.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="cancellationToken">A token that can cancel the update.</param>
    /// <returns>The effective settings written to the <c>ReferenceDocs</c> section.</returns>
    internal static async Task<ResolvedReferenceDocsSettings> ApplyAsync(
        DocsEnableHostingOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Directory.Exists(options.RootPath))
        {
            throw new DirectoryNotFoundException(
                $"Could not find repository root '{options.RootPath}'.");
        }

        if (!File.Exists(options.AppSettingsPath))
        {
            throw new FileNotFoundException(
                $"Could not find appsettings file '{options.AppSettingsPath}'.",
                options.AppSettingsPath);
        }

        var rootObject = await ReadRootObjectAsync(options.AppSettingsPath, cancellationToken);
        var referenceDocs = rootObject["ReferenceDocs"] as JsonObject ?? new JsonObject();
        rootObject["ReferenceDocs"] = referenceDocs;

        var effectiveRoutePrefix = ResolveRoutePrefix(options.RoutePrefix, referenceDocs);
        var effectiveDirectoryPath = ResolveDirectoryPath(options, referenceDocs);
        var effectiveDefaultDocument = ResolveDefaultDocument(options.DefaultDocument, referenceDocs);

        referenceDocs["Enabled"] = true;
        referenceDocs["RoutePrefix"] = effectiveRoutePrefix;
        referenceDocs["DirectoryPath"] = effectiveDirectoryPath;
        referenceDocs["DefaultDocument"] = effectiveDefaultDocument;

        await WriteRootObjectAsync(options.AppSettingsPath, rootObject, cancellationToken);

        return new ResolvedReferenceDocsSettings(
            RoutePrefix: effectiveRoutePrefix,
            DirectoryPath: effectiveDirectoryPath,
            DefaultDocument: effectiveDefaultDocument);
    }

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="DocsEnableHostingOptions" /> instance.
    /// </summary>
    /// <param name="args">The raw arguments that follow the <c>docs enable-hosting</c> command.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded.</param>
    /// <param name="error">When this method returns, contains the parse error if parsing failed.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    internal static bool TryParse(
        string[] args,
        out DocsEnableHostingOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        var rootPath = Directory.GetCurrentDirectory();
        string? appSettingsPath = null;
        string? directoryPath = null;
        string? routePrefix = null;
        string? defaultDocument = null;

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

        options = new DocsEnableHostingOptions
        {
            RootPath = rootPath,
            AppSettingsPath = appSettingsPath ?? Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
            DirectoryPath = directoryPath,
            RoutePrefix = routePrefix,
            DefaultDocument = defaultDocument
        };
        return true;
    }

    private static async Task<JsonObject> ReadRootObjectAsync(
        string appSettingsPath,
        CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(appSettingsPath, cancellationToken);
        var parsed = string.IsNullOrWhiteSpace(json)
            ? new JsonObject()
            : JsonNode.Parse(json);

        if (parsed is null)
        {
            return new JsonObject();
        }

        if (parsed is not JsonObject rootObject)
        {
            throw new InvalidOperationException(
                $"The appsettings file '{appSettingsPath}' must contain a top-level JSON object.");
        }

        return rootObject;
    }

    private static Task WriteRootObjectAsync(
        string appSettingsPath,
        JsonObject rootObject,
        CancellationToken cancellationToken)
    {
        var content = rootObject.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        }) + Environment.NewLine;

        return File.WriteAllTextAsync(appSettingsPath, content, cancellationToken);
    }

    private static string ResolveRoutePrefix(string? requestedRoutePrefix, JsonObject referenceDocs)
    {
        var candidate = requestedRoutePrefix ?? ReadString(referenceDocs, "RoutePrefix") ?? DefaultRoutePrefix;
        candidate = candidate.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return DefaultRoutePrefix;
        }

        candidate = "/" + candidate.Trim('/');
        if (candidate == "/")
        {
            throw new InvalidOperationException("ReferenceDocs route prefix must resolve to a non-root path.");
        }

        return candidate;
    }

    private static string ResolveDirectoryPath(DocsEnableHostingOptions options, JsonObject referenceDocs)
    {
        if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
        {
            return options.DirectoryPath.Trim();
        }

        var existing = ReadString(referenceDocs, "DirectoryPath");
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var appSettingsDirectory = Path.GetDirectoryName(options.AppSettingsPath)
            ?? Directory.GetCurrentDirectory();
        var docsDirectory = Path.Combine(options.RootPath, "docs", "reference");
        return Path.GetRelativePath(appSettingsDirectory, docsDirectory);
    }

    private static string ResolveDefaultDocument(string? requestedDefaultDocument, JsonObject referenceDocs)
    {
        var candidate = requestedDefaultDocument ?? ReadString(referenceDocs, "DefaultDocument") ?? DefaultDocument;
        candidate = candidate.Trim();
        return string.IsNullOrWhiteSpace(candidate)
            ? DefaultDocument
            : candidate;
    }

    private static string? ReadString(JsonObject jsonObject, string propertyName)
    {
        return jsonObject[propertyName]?.GetValue<string>();
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
    /// Represents the effective hosted reference-doc settings resolved for an appsettings file update.
    /// </summary>
    /// <param name="RoutePrefix">The normalized route prefix that was written.</param>
    /// <param name="DirectoryPath">The directory path that was written.</param>
    /// <param name="DefaultDocument">The default document that was written.</param>
    internal sealed record ResolvedReferenceDocsSettings(
        string RoutePrefix,
        string DirectoryPath,
        string DefaultDocument);
}
