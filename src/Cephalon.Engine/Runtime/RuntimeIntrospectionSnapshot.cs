using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Combines the main operator-facing runtime views into a single payload.
/// </summary>
/// <param name="Manifest">The immutable manifest that describes the built runtime shape.</param>
/// <param name="Status">The current lifecycle status of the runtime.</param>
/// <param name="TechnologySurfaces">
/// The active technology-pack runtime surfaces visible to the runtime at the time the snapshot was created.
/// </param>
/// <remarks>
/// This snapshot is intended for tooling and operator surfaces that need one coherent view of the runtime
/// without issuing separate requests for manifest, status, and technology-pack details.
/// </remarks>
public sealed record RuntimeIntrospectionSnapshot(
    RuntimeManifest Manifest,
    RuntimeStatusSnapshot Status,
    IReadOnlyList<TechnologyRuntimeSurface> TechnologySurfaces);
