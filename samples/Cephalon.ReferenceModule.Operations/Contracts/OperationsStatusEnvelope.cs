namespace Cephalon.ReferenceModule.Operations.Contracts;

public sealed record OperationsStatusEnvelope(
    int InitializeCount,
    int StartCount,
    int StopCount,
    string CurrentPhase,
    DateTimeOffset? LastTransitionUtc,
    string Culture,
    string Message);
