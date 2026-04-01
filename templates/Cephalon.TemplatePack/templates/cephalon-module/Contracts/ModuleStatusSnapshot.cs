namespace CephalonTemplateModule.Contracts;

public sealed record ModuleStatusSnapshot(
    int InitializeCount,
    int StartCount,
    int StopCount,
    string CurrentPhase,
    DateTimeOffset? LastTransitionUtc);
