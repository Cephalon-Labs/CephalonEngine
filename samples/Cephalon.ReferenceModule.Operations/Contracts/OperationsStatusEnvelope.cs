namespace Cephalon.ReferenceModule.Operations.Contracts;

/// <summary>
/// Represents the lifecycle status returned by the reference operations module.
/// </summary>
/// <param name="InitializeCount">
/// The number of times the module recorded initialization.
/// </param>
/// <param name="StartCount">
/// The number of times the module recorded startup.
/// </param>
/// <param name="StopCount">
/// The number of times the module recorded shutdown.
/// </param>
/// <param name="CurrentPhase">
/// The most recent lifecycle phase observed by the module.
/// </param>
/// <param name="LastTransitionUtc">
/// The UTC timestamp of the latest lifecycle transition, when available.
/// </param>
/// <param name="Culture">
/// The culture used to localize the response message.
/// </param>
/// <param name="Message">
/// The localized operational status message.
/// </param>
public sealed record OperationsStatusEnvelope(
    int InitializeCount,
    int StartCount,
    int StopCount,
    string CurrentPhase,
    DateTimeOffset? LastTransitionUtc,
    string Culture,
    string Message);
