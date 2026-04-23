namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector execution-adapter answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates
{
    /// <summary>
    /// The execution runtime does not currently produce a managed-connector execution-adapter answer.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector still has one or more blockers before Cephalon can trust a provider execution adapter.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// Cephalon can describe the provider execution lane, but the write-path remains operator-owned.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Cephalon can describe the provider execution lane, but no matching provider execution adapter is currently registered.
    /// </summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// Cephalon can route the shared issuance lane through a matching provider execution adapter.
    /// </summary>
    public const string Ready = "ready";
}
