namespace Cephalon.Engine.Composition.Packages;

internal sealed record PackageProvenanceLoadRequest(
    string? SourceRepository,
    string? SourceRevision,
    string? BuildUri,
    string? StatementUri);
