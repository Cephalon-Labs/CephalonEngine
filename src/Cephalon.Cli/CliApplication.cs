using Cephalon.Cli.Commands;
using Cephalon.Cli.Console;

namespace Cephalon.Cli;

/// <summary>
/// Hosts the main command-dispatch entry point for the Cephalon CLI.
/// </summary>
public static class CliApplication
{
    /// <summary>
    /// Runs the CLI for the supplied arguments and writers.
    /// </summary>
    /// <param name="args">The command-line arguments to execute.</param>
    /// <param name="output">The writer used for standard output.</param>
    /// <param name="error">The writer used for error output.</param>
    /// <param name="cancellationToken">A token that can cancel CLI execution.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var console = new CliConsole(output, error);

        if (args.Length == 0 || HasHelpFlag(args))
        {
            await console.WriteOutputAsync(HelpText.Value, cancellationToken);
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "new" => await RunNewAsync(args[1..], console, cancellationToken),
                "docs" => await RunDocsAsync(args[1..], console, cancellationToken),
                _ => await WriteUnknownCommandAsync(args[0], console, cancellationToken)
            };
        }
        catch (Exception exception)
        {
            await console.WriteErrorAsync(exception.Message, cancellationToken);
            return 1;
        }
    }

    private static bool HasHelpFlag(string[] args)
    {
        return args.Any(argument =>
            string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<int> RunNewAsync(
        string[] args,
        CliConsole console,
        CancellationToken cancellationToken)
    {
        var parse = NewAppCommand.TryParse(args, out var options, out var parseError);
        if (!parse || options is null)
        {
            await console.WriteErrorAsync(parseError ?? "Invalid command arguments.", cancellationToken);
            return 1;
        }

        return await NewAppCommand.RunAsync(options, console, cancellationToken);
    }

    private static async Task<int> RunDocsAsync(
        string[] args,
        CliConsole console,
        CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            await console.WriteErrorAsync(
                $"The 'docs' command requires a subcommand.{Environment.NewLine}{Environment.NewLine}{HelpText.Value}",
                cancellationToken);
            return 1;
        }

        if (!string.Equals(args[0], "publish", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(args[0], "enable-hosting", StringComparison.OrdinalIgnoreCase))
            {
                var enableHostingParse = DocsEnableHostingCommand.TryParse(args[1..], out var enableHostingOptions, out var enableHostingError);
                if (!enableHostingParse || enableHostingOptions is null)
                {
                    await console.WriteErrorAsync(enableHostingError ?? "Invalid command arguments.", cancellationToken);
                    return 1;
                }

                return await DocsEnableHostingCommand.RunAsync(enableHostingOptions, console, cancellationToken);
            }

            if (string.Equals(args[0], "validate-hosting", StringComparison.OrdinalIgnoreCase))
            {
                var validateHostingParse = DocsValidateHostingCommand.TryParse(args[1..], out var validateHostingOptions, out var validateHostingError);
                if (!validateHostingParse || validateHostingOptions is null)
                {
                    await console.WriteErrorAsync(validateHostingError ?? "Invalid command arguments.", cancellationToken);
                    return 1;
                }

                return await DocsValidateHostingCommand.RunAsync(validateHostingOptions, console, cancellationToken);
            }

            await console.WriteErrorAsync(
                $"Unknown docs subcommand '{args[0]}'.{Environment.NewLine}{Environment.NewLine}{HelpText.Value}",
                cancellationToken);
            return 1;
        }

        var parse = DocsPublishCommand.TryParse(args[1..], out var options, out var parseError);
        if (!parse || options is null)
        {
            await console.WriteErrorAsync(parseError ?? "Invalid command arguments.", cancellationToken);
            return 1;
        }

        return await DocsPublishCommand.RunAsync(options, console, cancellationToken);
    }

    private static async Task<int> WriteUnknownCommandAsync(
        string commandName,
        CliConsole console,
        CancellationToken cancellationToken)
    {
        await console.WriteErrorAsync(
            $"Unknown command '{commandName}'.{Environment.NewLine}{Environment.NewLine}{HelpText.Value}",
            cancellationToken);
        return 1;
    }

    private static class HelpText
    {
        public static string Value =>
            """
Cephalon CLI

Usage:
  cephalon new <AppName> [options]
  cephalon docs publish [options]
  cephalon docs enable-hosting [options]
  cephalon docs validate-hosting [options]

New options:
  --blueprint <name>         Blueprint to use. Default: ModularMonolith
  --module <name>            Module name to scaffold. Repeat for more modules.
  --feature <name>           Feature/slice name to scaffold. Repeat for more features.
  --transport <name>         Transport to enable. Repeat for more transports.
  --pattern <name>           Additional pattern to enable. Repeat for more patterns.
  --technology <name>        Technology profile to enable. Repeat for more technologies.
  --output <path>            Target directory. Default: ./<AppName>
  --package-version <ver>    Cephalon package version written to Directory.Packages.props.
  --target-framework <tfm>   Target framework for generated projects. Default: net10.0
  --force                    Overwrite existing files.

Docs publish options:
  --root <path>              Repository root. Default: current directory
  --output <path>            Output directory. Default: <root>/docs/reference
  --configuration <name>     Build configuration to read from. Default: Debug
  --target-framework <tfm>   Target framework to read from. Default: net10.0
  --assembly <name>          Assembly to document. Repeat for more assemblies.
  --no-overwrite             Fail if an output file already exists.
  --enable-hosting           Also enable ReferenceDocs hosting in the target appsettings file.
  --validate-hosting         Validate ReferenceDocs hosting in the target appsettings file after publishing.
  --appsettings <path>       Appsettings file used by --enable-hosting and --validate-hosting.
  --directory <path>         ReferenceDocs directory path override when --enable-hosting is used.
  --route-prefix <path>      ReferenceDocs route prefix override when --enable-hosting is used.
  --default-document <name>  ReferenceDocs default document override when --enable-hosting is used.
  --open                     Open the generated docs after publishing.
  --host-url <url>           Host base URL used by --open and --validate-hosting for hosted routes.

Docs enable-hosting options:
  --appsettings <path>       Appsettings file to update. Default: ./appsettings.json
  --root <path>              Repository root used to derive docs/reference paths. Default: current directory
  --directory <path>         ReferenceDocs directory path override.
  --route-prefix <path>      ReferenceDocs route prefix override.
  --default-document <name>  ReferenceDocs default document override.

Docs validate-hosting options:
  --appsettings <path>       Appsettings file to inspect. Default: ./appsettings.json
  --host-url <url>           Optional host base URL used to print the expected hosted routes.

  --help, -h                 Show this help.
""";
    }
}
