namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable metadata keys written by tenant-administration workflow commands.
/// </summary>
public static class TenantAdministrationWorkflowMetadataKeys
{
    /// <summary>
    /// Metadata key containing the last tenant-administration command.
    /// </summary>
    public const string LastAdministrationCommand = "lastAdministrationCommand";

    /// <summary>
    /// Metadata key containing the last tenant-administration command outcome.
    /// </summary>
    public const string LastAdministrationOutcome = "lastAdministrationOutcome";

    /// <summary>
    /// Metadata key containing the UTC timestamp when the last tenant-administration command was evaluated.
    /// </summary>
    public const string LastAdministrationOccurredAtUtc = "lastAdministrationOccurredAtUtc";

    /// <summary>
    /// Metadata key containing the actor that requested the last tenant-administration command.
    /// </summary>
    public const string LastAdministrationActor = "lastAdministrationActor";

    /// <summary>
    /// Metadata key containing the operator-facing reason for the last tenant-administration command.
    /// </summary>
    public const string LastAdministrationReason = "lastAdministrationReason";

    /// <summary>
    /// Metadata key containing the correlation identifier for the last tenant-administration command.
    /// </summary>
    public const string LastAdministrationCorrelationId = "lastAdministrationCorrelationId";

    /// <summary>
    /// Metadata key describing who owns the tenant-administration workflow that wrote the descriptor.
    /// </summary>
    public const string AdministrationWorkflowOwnership = "administrationWorkflowOwnership";
}
