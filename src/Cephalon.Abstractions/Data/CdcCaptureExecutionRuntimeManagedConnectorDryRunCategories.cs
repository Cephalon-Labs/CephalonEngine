namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector dry-run category identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories
{
    /// <summary>
    /// The managed connector is blocked by failed or otherwise blocking runtime remediation work.
    /// </summary>
    public const string BlockingRemediation = "blocking-remediation";

    /// <summary>
    /// The managed connector still needs runtime remediation attention before a dry-run answer can be trusted.
    /// </summary>
    public const string RuntimeRemediation = "runtime-remediation";

    /// <summary>
    /// The managed connector does not yet have full declared-versus-reported coverage on the shared runtime surface.
    /// </summary>
    public const string IncompleteReportingCoverage = "incomplete-reporting-coverage";

    /// <summary>
    /// The managed connector is currently out of policy for future write-path follow-through.
    /// </summary>
    public const string GovernanceOutOfPolicy = "governance-out-of-policy";

    /// <summary>
    /// The managed connector does not yet report enough runtime truth to trust a dry-run answer.
    /// </summary>
    public const string RuntimeTruthIncomplete = "runtime-truth-incomplete";

    /// <summary>
    /// The managed connector currently stays in observe-only mode.
    /// </summary>
    public const string ObserveOnlyMode = "observe-only-mode";

    /// <summary>
    /// The managed connector currently reports no shared write-path changes for the intended management operation.
    /// </summary>
    public const string NoChangesRequired = "no-changes-required";

    /// <summary>
    /// The managed connector currently reports at least one potential shared write-path change for the intended management operation.
    /// </summary>
    public const string ChangePlanned = "change-planned";

    /// <summary>
    /// The managed connector would require a lifecycle operation such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = "lifecycle-change";

    /// <summary>
    /// The managed connector would change the reported connector-cluster identifier to match the declared baseline.
    /// </summary>
    public const string ConnectClusterChange = "connect-cluster-change";

    /// <summary>
    /// The managed connector would change the reported connector class to match the declared baseline.
    /// </summary>
    public const string ConnectorClassChange = "connector-class-change";

    /// <summary>
    /// The managed connector would change the reported source-provider identifier to match the declared baseline.
    /// </summary>
    public const string SourceProviderChange = "source-provider-change";

    /// <summary>
    /// The managed connector would change the reported task topology to match the declared baseline.
    /// </summary>
    public const string TaskTopologyChange = "task-topology-change";
}
