using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Combines the main operator-facing runtime views into a single payload.
/// </summary>
/// <param name="Manifest">The immutable manifest that describes the built runtime shape.</param>
/// <param name="Status">The current lifecycle status of the runtime.</param>
/// <param name="ExecutionGraphs">
/// The execution graphs contributed by active modules and visible to the runtime at the time the snapshot was created.
/// </param>
/// <param name="TechnologySurfaces">
/// The active technology-pack runtime surfaces visible to the runtime at the time the snapshot was created.
/// </param>
/// <param name="DiagnosticsConventions">
/// The diagnostics conventions and published event-id catalogs visible to the runtime at the time the snapshot was created.
/// </param>
/// <param name="OperationalStory">
/// The richer operator-facing lifecycle story that combines loaded packages, execution-graph state, hosted-execution state, module state, and the ordered runtime timeline.
/// </param>
/// <remarks>
/// This snapshot is intended for tooling and operator surfaces that need one coherent view of the runtime
/// without issuing separate requests for manifest, status, execution-graph details, hosted-execution details, technology-pack details,
/// diagnostics conventions, data projection details, outbox details, inbox details, authorization-policy details, and lifecycle story data.
/// </remarks>
public sealed record RuntimeIntrospectionSnapshot(
    RuntimeManifest Manifest,
    RuntimeStatusSnapshot Status,
    IReadOnlyList<ExecutionGraphDescriptor> ExecutionGraphs,
    IReadOnlyList<TechnologyRuntimeSurface> TechnologySurfaces,
    IReadOnlyList<DiagnosticsConvention> DiagnosticsConventions,
    RuntimeOperationalStory OperationalStory)
{
    /// <summary>
    /// Gets the hosted executions contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<HostedExecutionDescriptor> HostedExecutions { get; init; } = [];

    /// <summary>
    /// Gets the projections contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<ProjectionDescriptor> Projections { get; init; } = [];

    /// <summary>
    /// Gets the outbox surfaces contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<OutboxDescriptor> Outboxes { get; init; } = [];

    /// <summary>
    /// Gets the inbox surfaces contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<InboxDescriptor> Inboxes { get; init; } = [];

    /// <summary>
    /// Gets the authorization policies contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<AuthorizationPolicyDescriptor> AuthorizationPolicies { get; init; } = [];

    /// <summary>
    /// Gets the audit-store surfaces contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<AuditStoreDescriptor> AuditStores { get; init; } = [];
}
