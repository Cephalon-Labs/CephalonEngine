using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;

namespace Cephalon.Engine.Runtime;

internal sealed class RuntimeIntrospectionSnapshotProvider(
    IRuntime runtime,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog,
    IRuntimeDiagnosticsCatalog diagnosticsCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
        return new RuntimeIntrospectionSnapshot(
            runtime.Manifest,
            runtime.StatusSnapshot,
            technologyRuntimeCatalog.Surfaces,
            diagnosticsCatalog.Conventions,
            runtime.OperationalStory);
    }
}
