using Cephalon.Cli;
using Cephalon.ReferenceDocs;

namespace Cephalon.Tests.Tooling;

public sealed class PackageSurfaceTests
{
    [Fact]
    public void CliAssemblyExposesOnlyTheTopLevelApplicationEntryPoint()
    {
        var exportedTypes = typeof(CliApplication)
            .Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["Cephalon.Cli.CliApplication"],
            exportedTypes);
    }

    [Fact]
    public void ReferenceDocsAssemblyExposesOnlyTheDocumentedLibrarySurface()
    {
        var exportedTypes = typeof(ReferenceDocsApplication)
            .Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "Cephalon.ReferenceDocs.Generation.ReferenceDocFile",
                "Cephalon.ReferenceDocs.Generation.ReferenceDocsGenerator",
                "Cephalon.ReferenceDocs.Generation.ReferenceDocsRequest",
                "Cephalon.ReferenceDocs.Generation.RenderedReferenceDocs",
                "Cephalon.ReferenceDocs.IO.ReferenceDocsWriter",
                "Cephalon.ReferenceDocs.ReferenceDocsApplication"
            ],
            exportedTypes);
    }
}
