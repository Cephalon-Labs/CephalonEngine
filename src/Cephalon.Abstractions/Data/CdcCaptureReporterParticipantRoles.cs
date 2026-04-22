namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable participant-role identifiers used by CDC reporter-coordination stories.
/// </summary>
public static class CdcCaptureReporterParticipantRoles
{
    /// <summary>
    /// The reporter currently holds one active lease for the execution runtime.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// The reporter is still visible through accepted runtime observations, but does not currently hold the active lease.
    /// </summary>
    public const string Standby = "standby";

    /// <summary>
    /// The reporter most recently attempted to report while another reporter still owned the active lease.
    /// </summary>
    public const string Rejected = "rejected";
}
