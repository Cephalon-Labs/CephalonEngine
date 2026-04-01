namespace Cephalon.Engine.Runtime;

internal sealed class ModulePhaseException : Exception
{
    public ModulePhaseException(
        string phase,
        string moduleId,
        string moduleVersion,
        Exception innerException)
        : base(
            $"Module '{moduleId}' failed during phase '{phase}'.",
            innerException)
    {
        Phase = phase;
        ModuleId = moduleId;
        ModuleVersion = moduleVersion;
    }

    public string Phase { get; }

    public string ModuleId { get; }

    public string ModuleVersion { get; }
}
