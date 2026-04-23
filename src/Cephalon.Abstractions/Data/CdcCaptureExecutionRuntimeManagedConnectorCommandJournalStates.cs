namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector command-journal answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates
{
    /// <summary>
    /// The command journal does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The command journal currently has no recorded managed-connector command outcomes.
    /// </summary>
    public const string Empty = "empty";

    /// <summary>
    /// The command journal currently retains bounded recent history and the retained evidence is sufficient for operator-facing automation answers.
    /// </summary>
    public const string Bounded = "bounded";

    /// <summary>
    /// The command journal currently retains only the newest bounded command history because older entries were truncated.
    /// </summary>
    public const string Truncated = "truncated";

    /// <summary>
    /// The command journal currently retains matching recent command evidence that is still inside the retry cooldown window.
    /// </summary>
    public const string CooldownActive = "cooldown-active";

    /// <summary>
    /// The command journal currently retains matching command evidence showing that replaying the command would be duplicative.
    /// </summary>
    public const string DuplicateEvidencePresent = "duplicate-evidence-present";

    /// <summary>
    /// The command journal currently retains history, but the retained evidence is still insufficient for automatic execution or background automation.
    /// </summary>
    public const string InsufficientForAutomation = "insufficient-for-automation";
}
