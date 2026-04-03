using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;

namespace Cephalon.Engine.Runtime;

internal sealed class RuntimeIntrospectionSnapshotProvider(
    IRuntime runtime,
    IExecutionRuntimeCatalog executionRuntimeCatalog,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog,
    IRuntimeDiagnosticsCatalog diagnosticsCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
        return new RuntimeIntrospectionSnapshot(
            runtime.Manifest,
            runtime.StatusSnapshot,
            executionRuntimeCatalog.Graphs,
            technologyRuntimeCatalog.Surfaces,
            diagnosticsCatalog.Conventions,
            runtime.OperationalStory);
    }
}
