namespace Cephalon.Eventing.Services;

internal interface IEventingBenchmarkProofCatalog
{
    IReadOnlyList<EventingBenchmarkProofDescriptor> Guardrails { get; }
}

internal sealed class EventingBenchmarkProofCatalog : IEventingBenchmarkProofCatalog
{
    public const string GuardrailCatalogReference = "benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json";
    public const string RequiredGuardrailFamilies = "remediation-filtered-reads,cold-start,broker-dispatch,durable-journal,provider-managed-eventing,provider-operated-eventing";

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
            Id: "eventing.broker-dispatch.report-projected-headers",
            Family: "broker-dispatch",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchBrokerDispatchBenchmarks-report.csv",
            Benchmark: "ReportProjectedBrokerDispatches",
            MaxMeanNanoseconds: 10000,
            MaxAllocatedBytes: 5000,
            HotPath: "provider-neutral-broker-dispatch-report"),
        new(
            Id: "eventing.durable-journal.record-replay-cursor",
            Family: "durable-journal",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventDispatchDurableJournalBenchmarks-report.csv",
            Benchmark: "RecordAndReadDurableJournal",
            MaxMeanNanoseconds: 500000,
            MaxAllocatedBytes: 262144,
            HotPath: "durable-command-journal-record-replay-cursor"),
        new(
            Id: "eventing.provider-managed.report-runtime-proofs",
            Family: "provider-managed-eventing",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventProviderManagedEventingBenchmarks-report.csv",
            Benchmark: "ReportProviderManagedEventingProofs",
            MaxMeanNanoseconds: 20000,
            MaxAllocatedBytes: 65536,
            HotPath: "provider-managed-dispatch-subscription-proof-report"),
        new(
            Id: "eventing.provider-operated.report-aggregate-runtime-proofs",
            Family: "provider-operated-eventing",
            ReportFileName: "Cephalon.Benchmarks.HotPath.EventProviderOperatedEventingBenchmarks-report.csv",
            Benchmark: "ReportProviderOperatedEventingProofs",
            MaxMeanNanoseconds: 50000,
            MaxAllocatedBytes: 131072,
            HotPath: "provider-operated-aggregate-proof-report")
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
