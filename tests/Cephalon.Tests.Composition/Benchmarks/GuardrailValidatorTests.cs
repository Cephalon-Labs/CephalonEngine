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
        var actualBenchmarkIds = catalog.Entries
            .Select(entry => entry.Benchmark)
            .ToHashSet(StringComparer.Ordinal);
        var actualBenchmarkEntryIds = catalog.Entries
            .Select(entry => $"{entry.ReportFileName}|{entry.Benchmark}")
            .ToHashSet(StringComparer.Ordinal);

        var expectedBenchmarkIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "BuildRuntimeManifest",
            "BuildRuntimeManifestWithStrictTrustPolicy",
            "BuildPhase8RuntimeManifest",
            "InitializeStartStopRuntime",
            "InitializeStartStopPhase8Runtime",
            "HandleLoggedJsonRequest",
            "HandleTruncatedJsonRequest",
            "HandleConcurrentLoggedJsonRequest",
            "BuildMapGovernedRestCatalogs",
            "GenerateBlueprintScaffold",
            "GeneratePhase8BlueprintScaffold",
            "DispatchQuery",
            "DispatchCommand",
            "DispatchCommandWithResult",
            "DispatchBehavior",
            "EvaluateRbacAllow",
            "EvaluateRbacDeny",
            "ResolveByTenantId",
            "ResolveByHostName",
            "ResolveDefaultTenant",
            "AppendSingleEvent",
            "ReadStream",
            "GetStreamVersion",
            "StageOutboxMessage"
        };
        var expectedBenchmarkEntryIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "Cephalon.Benchmarks.Composition.EngineBuilderBenchmarks-report.csv|BuildRuntimeManifest",
            "Cephalon.Benchmarks.Composition.EngineBuilderBenchmarks-report.csv|BuildRuntimeManifestWithStrictTrustPolicy",
            "Cephalon.Benchmarks.Composition.EngineBuilderBenchmarks-report.csv|BuildPhase8RuntimeManifest",
            "Cephalon.Benchmarks.Runtime.EngineRuntimeBenchmarks-report.csv|InitializeStartStopRuntime",
            "Cephalon.Benchmarks.Runtime.EngineRuntimeBenchmarks-report.csv|InitializeStartStopPhase8Runtime",
            "Cephalon.Benchmarks.Runtime.AspNetCoreRequestLoggingBenchmarks-report.csv|HandleLoggedJsonRequest",
            "Cephalon.Benchmarks.Runtime.AspNetCoreRequestLoggingBenchmarks-report.csv|HandleTruncatedJsonRequest",
            "Cephalon.Benchmarks.Runtime.AspNetCoreRequestLoggingBenchmarks-report.csv|HandleConcurrentLoggedJsonRequest",
            "Cephalon.Benchmarks.Runtime.RestEndpointProjectionGovernanceBenchmarks-report.csv|BuildMapGovernedRestCatalogs",
            "Cephalon.Benchmarks.Scaffolding.ScaffoldGeneratorBenchmarks-report.csv|GenerateBlueprintScaffold",
            "Cephalon.Benchmarks.Scaffolding.ScaffoldGeneratorBenchmarks-report.csv|GeneratePhase8BlueprintScaffold",
            "Cephalon.Benchmarks.HotPath.DataDispatchBenchmarks-report.csv|DispatchQuery",
            "Cephalon.Benchmarks.HotPath.DataDispatchBenchmarks-report.csv|DispatchCommand",
            "Cephalon.Benchmarks.HotPath.DataDispatchBenchmarks-report.csv|DispatchCommandWithResult",
            "Cephalon.Benchmarks.HotPath.BehaviorDispatchBenchmarks-report.csv|DispatchBehavior",
            "Cephalon.Benchmarks.HotPath.AuthorizationEvaluationBenchmarks-report.csv|EvaluateRbacAllow",
            "Cephalon.Benchmarks.HotPath.AuthorizationEvaluationBenchmarks-report.csv|EvaluateRbacDeny",
            "Cephalon.Benchmarks.HotPath.TenantResolutionBenchmarks-report.csv|ResolveByTenantId",
            "Cephalon.Benchmarks.HotPath.TenantResolutionBenchmarks-report.csv|ResolveByHostName",
            "Cephalon.Benchmarks.HotPath.TenantResolutionBenchmarks-report.csv|ResolveDefaultTenant",
            "Cephalon.Benchmarks.HotPath.EventSourcingBenchmarks-report.csv|AppendSingleEvent",
            "Cephalon.Benchmarks.HotPath.EventSourcingBenchmarks-report.csv|ReadStream",
            "Cephalon.Benchmarks.HotPath.EventSourcingBenchmarks-report.csv|GetStreamVersion",
            "Cephalon.Benchmarks.HotPath.OutboxStagingBenchmarks-report.csv|StageOutboxMessage"
        };

        var missingBenchmarkIds = expectedBenchmarkIds
            .Except(actualBenchmarkIds)
            .OrderBy(id => id)
            .ToArray();
        var unexpectedBenchmarkIds = actualBenchmarkIds
            .Except(expectedBenchmarkIds)
            .OrderBy(id => id)
            .ToArray();
        var missingBenchmarkEntryIds = expectedBenchmarkEntryIds
            .Except(actualBenchmarkEntryIds)
            .OrderBy(id => id)
            .ToArray();
        var unexpectedBenchmarkEntryIds = actualBenchmarkEntryIds
            .Except(expectedBenchmarkEntryIds)
            .OrderBy(id => id)
            .ToArray();

        Assert.True(
            missingBenchmarkIds.Length == 0,
            $"Guardrail catalog is missing expected benchmarks: {string.Join(", ", missingBenchmarkIds)}");
        Assert.True(
            unexpectedBenchmarkIds.Length == 0,
            $"Guardrail catalog has undocumented benchmark entries: {string.Join(", ", unexpectedBenchmarkIds)}");
        Assert.True(
            missingBenchmarkEntryIds.Length == 0,
            $"Guardrail catalog is missing expected report+benchmark pairs: {string.Join(", ", missingBenchmarkEntryIds)}");
        Assert.True(
            unexpectedBenchmarkEntryIds.Length == 0,
            $"Guardrail catalog has unexpected report+benchmark pairs: {string.Join(", ", unexpectedBenchmarkEntryIds)}");

        Assert.Equal(expectedBenchmarkIds.Count, actualBenchmarkIds.Count);
        Assert.Equal(expectedBenchmarkEntryIds.Count, actualBenchmarkEntryIds.Count);
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
