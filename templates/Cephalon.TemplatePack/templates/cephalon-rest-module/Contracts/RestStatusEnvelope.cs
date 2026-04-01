namespace CephalonTemplateModule.Contracts;

public sealed record RestStatusEnvelope(
    int InitializeCount,
    int StartCount,
    int StopCount,
    string CurrentPhase,
    DateTimeOffset? LastTransitionUtc,
    string Culture,
    string Message);
