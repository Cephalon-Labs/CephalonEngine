using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;

namespace Cephalon.ReferenceDocs;

/// <summary>
/// Hosts the main command-dispatch entry point for the Cephalon reference docs generator.
/// </summary>
public static class ReferenceDocsApplication
{
    /// <summary>
    /// Runs the reference docs generator for the supplied arguments and writers.
    /// </summary>
    /// <param name="args">The command-line arguments to execute.</param>
    /// <param name="output">The writer used for standard output.</param>
    /// <param name="error">The writer used for error output.</param>
    /// <param name="cancellationToken">A token that can cancel execution.</param>
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

        if (args.Length > 0 && HasHelpFlag(args))
        {
            await output.WriteLineAsync(HelpText);
            return 0;
        }

        try
        {
            var request = Parse(args);
            var rendered = ReferenceDocsGenerator.Generate(request);
            await ReferenceDocsWriter.WriteAsync(rendered, overwrite: true, cancellationToken);

            await output.WriteLineAsync(
                $"Generated {rendered.Files.Count} reference doc files in '{request.OutputPath}'.");
            return 0;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync(exception.Message);
            return 1;
        }
    }

    private static ReferenceDocsRequest Parse(string[] args)
    {
        var rootPath = Directory.GetCurrentDirectory();
        string? outputPath = null;
        var configuration = "Debug";
        var targetFramework = "net10.0";
        var assemblies = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--root":
                    rootPath = ReadValue(args, ref index, "--root");
                    break;
                case "--output":
                    outputPath = ReadValue(args, ref index, "--output");
                    break;
                case "--configuration":
                    configuration = ReadValue(args, ref index, "--configuration");
                    break;
                case "--target-framework":
                    targetFramework = ReadValue(args, ref index, "--target-framework");
                    break;
                case "--assembly":
                    assemblies.Add(ReadValue(args, ref index, "--assembly"));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown option '{argument}'.{Environment.NewLine}{Environment.NewLine}{HelpText}");
            }
        }

        var normalizedRoot = Path.GetFullPath(rootPath);
        var normalizedOutput = Path.GetFullPath(outputPath ?? Path.Combine(normalizedRoot, "docs", "reference"));

        return new ReferenceDocsRequest(
            rootPath: normalizedRoot,
            outputPath: normalizedOutput,
            configuration: configuration,
            targetFramework: targetFramework,
            assemblies: assemblies);
    }

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            throw new InvalidOperationException($"Option '{optionName}' requires a value.");
        }

        index++;
        return args[index].Trim();
    }

    private static bool HasHelpFlag(string[] args)
    {
        return args.Any(argument =>
            string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase));
    }

    private const string HelpText =
        """
Reference docs generator

Usage:
  dotnet run --project src/Cephalon.ReferenceDocs -- [options]

Options:
  --root <path>                Repository root. Default: current directory
  --output <path>              Output directory. Default: <root>/docs/reference
  --configuration <name>       Build configuration to read from. Default: Debug
  --target-framework <tfm>     Target framework to read from. Default: net10.0
  --assembly <name>            Assembly to document. Repeat for more assemblies.
  --help, -h                   Show this help.
""";
}
