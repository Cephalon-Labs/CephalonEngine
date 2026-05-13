namespace Cephalon.Eventing.Services;

internal interface IEventingBenchmarkProofCatalog
{
    IReadOnlyList<EventingBenchmarkProofDescriptor> Guardrails { get; }
}

internal sealed class EventingBenchmarkProofCatalog : IEventingBenchmarkProofCatalog
{
    public const string GuardrailCatalogReference = "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json";
    public const string RequiredGuardrailFamilies = "remediation-filtered-reads,cold-start,broker-dispatch,durable-journal,provider-managed-eventing";

    private static readonly IReadOnlyList<EventingBenchmarkProofDescriptor> DefaultGuardrails =
    [
        new(
            Id: "eventing.remediation.filter-summary-by-message-id",
            Family: "remediation-filtered-reads",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchRemediationCatalogBenchmarks-report.csv",
            Benchmark: "FilterSummaryByMessageId",
            MaxMeanNanoseconds: 8000,
            MaxAllocatedBytes: 512,
            HotPath: "summary-filter-single-pass"),
        new(
            Id: "eventing.remediation.filter-retention-by-message-id",
            Family: "remediation-filtered-reads",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchRemediationCatalogBenchmarks-report.csv",
            Benchmark: "FilterRetentionByMessageId",
            MaxMeanNanoseconds: 8000,
            MaxAllocatedBytes: 512,
            HotPath: "retention-filter-single-pass"),
        new(
            Id: "eventing.remediation.filter-latest-by-correlation-id",
            Family: "remediation-filtered-reads",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchRemediationCatalogBenchmarks-report.csv",
            Benchmark: "FilterLatestByCorrelationId",
            MaxMeanNanoseconds: 15000,
            MaxAllocatedBytes: 0,
            HotPath: "latest-filter-single-pass"),
        new(
            Id: "eventing.remediation.filter-oldest-by-dispatch-outcome",
            Family: "remediation-filtered-reads",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchRemediationCatalogBenchmarks-report.csv",
            Benchmark: "FilterOldestByDispatchOutcome",
            MaxMeanNanoseconds: 2000,
            MaxAllocatedBytes: 0,
            HotPath: "oldest-filter-single-pass"),
        new(
            Id: "eventing.remediation.filter-operator-dashboard-selectors",
            Family: "remediation-filtered-reads",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchRemediationCatalogBenchmarks-report.csv",
            Benchmark: "FilterOperatorDashboardSelectors",
            MaxMeanNanoseconds: 30000,
            MaxAllocatedBytes: 512,
            HotPath: "operator-dashboard-multi-selector"),
        new(
            Id: "eventing.host-cold-start.aspnetcore-core-operator",
            Family: "cold-start",
            ReportFileName: "Cephalon.Benchmarks.Runtime.ColdStartBenchmarks-report.csv",
            Benchmark: "BuildStartHandleFirstRequestAspNetCore",
            MaxMeanNanoseconds: 800000000,
            MaxAllocatedBytes: 30000000,
            HotPath: "aspnetcore-core-operator-cold-start"),
        new(
            Id: "eventing.host-cold-start.worker",
            Family: "cold-start",
            ReportFileName: "Cephalon.Benchmarks.Runtime.ColdStartBenchmarks-report.csv",
            Benchmark: "BuildStartWorkerHost",
            MaxMeanNanoseconds: 5000000,
            MaxAllocatedBytes: 500000,
            HotPath: "worker-host-cold-start"),
        new(
            Id: "eventing.event-journal.append-single-event",
            Family: "durable-journal",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventSourcingBenchmarks-report.csv",
            Benchmark: "AppendSingleEvent",
            MaxMeanNanoseconds: 10000,
            MaxAllocatedBytes: 10000,
            HotPath: "event-journal-single-append"),
        new(
            Id: "eventing.event-journal.read-stream",
            Family: "durable-journal",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventSourcingBenchmarks-report.csv",
            Benchmark: "ReadStream",
            MaxMeanNanoseconds: 50000,
            MaxAllocatedBytes: 20000,
            HotPath: "event-journal-stream-read"),
        new(
            Id: "eventing.event-journal.get-stream-version",
            Family: "durable-journal",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventSourcingBenchmarks-report.csv",
            Benchmark: "GetStreamVersion",
            MaxMeanNanoseconds: 5000,
            MaxAllocatedBytes: 5000,
            HotPath: "event-journal-version-lookup")
    ];

    public IReadOnlyList<EventingBenchmarkProofDescriptor> Guardrails => DefaultGuardrails;
}

internal sealed record EventingBenchmarkProofDescriptor(
    string Id,
    string Family,
    string ReportFileName,
    string Benchmark,
    long MaxMeanNanoseconds,
    long MaxAllocatedBytes,
    string HotPath);
