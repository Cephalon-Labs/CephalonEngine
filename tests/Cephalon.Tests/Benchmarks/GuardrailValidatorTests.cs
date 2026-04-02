using Cephalon.Benchmarks.Validation;

namespace Cephalon.Tests.Benchmarks;

public sealed class GuardrailValidatorTests
{
    [Fact]
    public void RepositoryGuardrailCatalogContainsEntriesForShippedBenchmarks()
    {
        var catalog = GuardrailCatalog.Load(GetRepositoryFile(
            "benchmarks",
            "Cephalon.Benchmarks",
            "guardrails",
            "performance-guardrails.json"));

        Assert.Equal("1.0", catalog.Version);
        Assert.Equal(4, catalog.Entries.Count);
        Assert.Contains(catalog.Entries, entry => entry.Benchmark == "BuildRuntimeManifest");
        Assert.Contains(catalog.Entries, entry => entry.Benchmark == "InitializeStartStopRuntime");
        Assert.Contains(catalog.Entries, entry => entry.Benchmark == "HandleLoggedJsonRequest");
        Assert.Contains(catalog.Entries, entry => entry.Benchmark == "GenerateBlueprintScaffold");
    }

    [Fact]
    public async Task CsvReaderParsesBenchmarkDotNetUnitsAndValidatorAcceptsHealthyResults()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var reportPath = Path.Combine(directory, "Sample-report.csv");
            await File.WriteAllTextAsync(reportPath, """
Method,Mean,Allocated
ComposeEngine,11.50 μs,24.00 KB
""");

            var catalog = new GuardrailCatalog("1.0",
            [
                new GuardrailEntry(
                    ReportFileName: "Sample-report.csv",
                    Benchmark: "ComposeEngine",
                    MaxMeanNanoseconds: 12_000,
                    MaxAllocatedBytes: 30_000,
                    Notes: null)
            ]);

            var measurements = CsvBenchmarkReportReader.ReadDirectory(directory);
            var result = GuardrailValidator.Validate(catalog, measurements);

            var measurement = Assert.Single(measurements);
            Assert.Equal("ComposeEngine", measurement.Benchmark);
            Assert.Equal(11_500d, measurement.MeanNanoseconds);
            Assert.True(measurement.AllocatedBytes > 24_000d);
            Assert.True(result.Passed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ValidatorReportsExceededMeanAndAllocationGuardrails()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var reportPath = Path.Combine(directory, "Sample-report.csv");
            await File.WriteAllTextAsync(reportPath, """
Method,Mean,Allocated
ComposeEngine,18.00 μs,64.00 KB
""");

            var catalog = new GuardrailCatalog("1.0",
            [
                new GuardrailEntry(
                    ReportFileName: "Sample-report.csv",
                    Benchmark: "ComposeEngine",
                    MaxMeanNanoseconds: 12_000,
                    MaxAllocatedBytes: 30_000,
                    Notes: null)
            ]);

            var measurements = CsvBenchmarkReportReader.ReadDirectory(directory);
            var result = GuardrailValidator.Validate(catalog, measurements);

            Assert.False(result.Passed);
            Assert.Contains(result.Messages, message => message.Contains("Mean", StringComparison.Ordinal));
            Assert.Contains(result.Messages, message => message.Contains("Allocation", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cephalon-benchmark-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find repository file '{Path.Combine(segments)}' from '{AppContext.BaseDirectory}'.");
    }
}
