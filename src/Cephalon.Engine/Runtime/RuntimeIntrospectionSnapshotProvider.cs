using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Runtime;

internal sealed class RuntimeIntrospectionSnapshotProvider(
    IRuntime runtime,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
        return new RuntimeIntrospectionSnapshot(
            runtime.Manifest,
            runtime.StatusSnapshot,
            technologyRuntimeCatalog.Surfaces);
    }
}
