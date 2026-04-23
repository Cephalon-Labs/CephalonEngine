namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable desired-versus-observed drift categories used by managed-connector CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDriftCategories
{
    /// <summary>
    /// The managed connector does not currently declare task ids or an expected task count.
    /// </summary>
    public const string MissingTaskBaseline = "missing-task-baseline";

    /// <summary>
    /// The managed connector has not yet reported task ids or a reported task count.
    /// </summary>
    public const string ReportedTaskTopologyUnavailable = "reported-task-topology-unavailable";

    /// <summary>
    /// The managed connector reports task counts, but not the task identities needed to compare declared task ids.
    /// </summary>
    public const string ReportedTaskIdentityUnavailable = "reported-task-identity-unavailable";

    /// <summary>
    /// The managed connector reports a different task count than the declared task baseline.
    /// </summary>
    public const string TaskCountMismatch = "task-count-mismatch";

    /// <summary>
    /// One or more declared task ids are missing from the latest reported task ids.
    /// </summary>
    public const string MissingDeclaredTaskReports = "missing-declared-task-reports";

    /// <summary>
    /// One or more reported task ids were not part of the declared task baseline.
    /// </summary>
    public const string UnexpectedReportedTasks = "unexpected-reported-tasks";

    /// <summary>
    /// The latest reported connector-cluster identifier differs from the declared managed-connector baseline.
    /// </summary>
    public const string ConnectClusterMismatch = "connect-cluster-mismatch";

    /// <summary>
    /// The latest reported connector class differs from the declared managed-connector baseline.
    /// </summary>
    public const string ConnectorClassMismatch = "connector-class-mismatch";

    /// <summary>
    /// The latest reported source-provider identifier differs from the declared managed-connector baseline.
    /// </summary>
    public const string SourceProviderMismatch = "source-provider-mismatch";
}
