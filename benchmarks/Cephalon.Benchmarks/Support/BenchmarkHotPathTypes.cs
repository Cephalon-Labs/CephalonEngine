using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Services;
using Cephalon.Identity.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Support;

// ───── Data Layer Stubs ─────

/// <summary>Trivial query that returns a deterministic integer for benchmark dispatch measurement.</summary>
internal sealed record BenchmarkQuery(int Value) : IQuery<int>;

/// <summary>Trivial void command for benchmark dispatch measurement.</summary>
internal sealed record BenchmarkCommand(int Value) : ICommand;

/// <summary>Trivial result-returning command for benchmark dispatch measurement.</summary>
internal sealed record BenchmarkResultCommand(int Value) : ICommand<int>;

/// <summary>No-op query handler returning the input doubled.</summary>
internal sealed class BenchmarkQueryHandler : IQueryHandler<BenchmarkQuery, int>
{
    public ValueTask<int> HandleAsync(BenchmarkQuery query, CancellationToken cancellationToken = default)
        => new(query.Value * 2);
}

/// <summary>No-op command handler completing synchronously.</summary>
internal sealed class BenchmarkCommandHandler : ICommandHandler<BenchmarkCommand>
{
    public ValueTask HandleAsync(BenchmarkCommand command, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}

/// <summary>No-op result command handler returning input + 1.</summary>
internal sealed class BenchmarkResultCommandHandler : ICommandHandler<BenchmarkResultCommand, int>
{
    public ValueTask<int> HandleAsync(BenchmarkResultCommand command, CancellationToken cancellationToken = default)
        => new(command.Value + 1);
}

// ───── Behavior Dispatch Stubs ─────

/// <summary>Minimal echo behavior returning the input string unchanged.</summary>
internal sealed class EchoBenchmarkBehavior : IAppBehavior<string, string>
{
    public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken ct = default)
        => Task.FromResult(input);
}

/// <summary>Minimal behavior context for benchmarking without HTTP infrastructure.</summary>
internal sealed class StubBehaviorContext(string behaviorId) : IBehaviorContext
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string BehaviorId { get; } = behaviorId;
    public string? CorrelationId => "bench-correlation";
    public IReadOnlyDictionary<string, string> Metadata => EmptyMetadata;
    public IEventStore? EventStore => null;

    public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Direct pattern does not support reply semantics.");
}

/// <summary>Minimal behavior catalog backed by an array of descriptors.</summary>
internal sealed class StubBehaviorCatalog(params BehaviorTopologyDescriptor[] descriptors) : IBehaviorCatalog
{
    public IReadOnlyList<BehaviorTopologyDescriptor> All { get; } = descriptors;

    public BehaviorTopologyDescriptor? FindById(string behaviorId)
        => All.FirstOrDefault(d => string.Equals(d.Id, behaviorId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<BehaviorTopologyDescriptor> GetByPattern(string pattern)
        => All.Where(d => string.Equals(d.Pattern, pattern, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<BehaviorTopologyDescriptor> GetByTransport(string transportId)
        => All.Where(d => d.TransportIds.Contains(transportId, StringComparer.OrdinalIgnoreCase)).ToList();
}

/// <summary>Minimal behavior type registry backed by a dictionary.</summary>
internal sealed class StubBehaviorTypeRegistry : IBehaviorTypeRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string behaviorId, Type behaviorType) => _map[behaviorId] = behaviorType;

    public bool TryGetType(string behaviorId, out Type? behaviorType)
        => _map.TryGetValue(behaviorId, out behaviorType);
}

// ───── Authorization Policy Module ─────

/// <summary>
/// Benchmark module that contributes RBAC authorization policies to the engine for hot-path evaluation measurement.
/// </summary>
internal sealed class BenchmarkAuthorizationPolicyModule : ModuleBase, IAuthorizationPolicyContributor
{
    /// <summary>The benchmark policy identifier that grants access to the "admin" role.</summary>
    internal const string AdminPolicyId = "benchmark-admin-policy";

    /// <summary>The benchmark policy identifier that requires the "superadmin" role (used for deny-path testing).</summary>
    internal const string SuperAdminPolicyId = "benchmark-superadmin-policy";

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "benchmark-authorization",
        displayName: "Benchmark Authorization",
        description: "Contributes authorization policies for hot-path benchmark scenarios.",
        tags: ["benchmark", "authorization"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "benchmark.authorization",
            displayName: "Benchmark Authorization",
            description: "Authorization policies for benchmark scenarios."));
    }

    public void RegisterPolicies(IAuthorizationPolicyRegistry policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        policies.Add(new AuthorizationPolicyDescriptor(
            id: AdminPolicyId,
            displayName: "Admin Access",
            description: "Grants access to subjects with the admin role.",
            modes: [AuthorizationMode.Rbac],
            tags: ["benchmark"],
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "admin",
                [IdentityPolicyMetadataKeys.RequiredRoleMatch] = IdentityPolicyMetadataKeys.RequiredRoleMatchAny
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: SuperAdminPolicyId,
            displayName: "Super Admin Access",
            description: "Grants access only to subjects with the superadmin role.",
            modes: [AuthorizationMode.Rbac],
            tags: ["benchmark"],
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [IdentityPolicyMetadataKeys.RequiredRoles] = "superadmin",
                [IdentityPolicyMetadataKeys.RequiredRoleMatch] = IdentityPolicyMetadataKeys.RequiredRoleMatchAny
            }));
    }
}
