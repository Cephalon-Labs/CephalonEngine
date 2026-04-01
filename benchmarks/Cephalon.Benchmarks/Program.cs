using BenchmarkDotNet.Running;
using Cephalon.Benchmarks.Validation;

if (TryRunGuardrailValidation(args))
{
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

static bool TryRunGuardrailValidation(string[] args)
{
    if (!args.Any(argument => string.Equals(argument, "--validate-guardrails", StringComparison.OrdinalIgnoreCase)))
    {
        return false;
    }

    var baselinePath = GetOptionValue(args, "--guardrails")
        ?? Path.Combine(AppContext.BaseDirectory, "guardrails", "performance-guardrails.json");
    var resultsDirectory = GetOptionValue(args, "--results")
        ?? Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");

    var catalog = GuardrailCatalog.Load(baselinePath);
    var measurements = CsvBenchmarkReportReader.ReadDirectory(resultsDirectory);
    var result = GuardrailValidator.Validate(catalog, measurements);

    foreach (var message in result.Messages)
    {
        Console.WriteLine(message);
    }

    Environment.ExitCode = result.Passed ? 0 : 1;
    return true;
}

static string? GetOptionValue(string[] args, string optionName)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    return null;
}
