using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
[AppBehavior("echo")]
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

// ───── In-Memory Event Store ─────

/// <summary>
/// Lightweight in-memory event store for benchmarking append, read, and version lookup
/// without external database dependencies. Provides correct optimistic concurrency semantics.
/// </summary>
internal sealed class InMemoryBenchmarkEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> _streams = new(StringComparer.Ordinal);
    private readonly object _appendLock = new();

    public Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        lock (_appendLock)
        {
            var stream = _streams.GetOrAdd(streamId, static _ => []);
            var currentVersion = stream.Count == 0 ? -1L : stream[^1].StreamVersion;

            if (currentVersion != expectedVersion)
            {
                throw new EventStreamConcurrencyException(streamId, expectedVersion, currentVersion);
            }

            stream.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var stream))
        {
            yield break;
        }

        foreach (var evt in stream)
        {
            if (evt.StreamVersion >= fromVersion)
            {
                yield return evt;
            }
        }

        await Task.CompletedTask;
    }

    public Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var stream) || stream.Count == 0)
        {
            return Task.FromResult(-1L);
        }

        return Task.FromResult(stream[^1].StreamVersion);
    }

    /// <summary>Resets all streams for benchmark iteration cleanup.</summary>
    public void Clear() => _streams.Clear();
}

/// <summary>Trivial domain event for event-sourcing benchmarks.</summary>
internal sealed class BenchmarkDomainEvent(string streamId, long streamVersion) : IDomainEvent
{
    public string StreamId { get; } = streamId;
    public long StreamVersion { get; } = streamVersion;
    public DateTime OccurredAtUtc { get; } = new(2042, 4, 2, 10, 0, 0, DateTimeKind.Utc);
}

// ───── In-Memory Outbox ─────

/// <summary>
/// Lightweight in-memory outbox for benchmarking enqueue staging without external database dependencies.
/// </summary>
internal sealed class InMemoryBenchmarkOutbox : IOutbox
{
    private readonly List<OutboxMessage> _messages = [];

    public string OutboxId => "benchmark-outbox";

    public ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return ValueTask.CompletedTask;
    }

    /// <summary>Gets the number of staged messages.</summary>
    public int Count => _messages.Count;

    /// <summary>Resets the outbox for benchmark iteration cleanup.</summary>
    public void Clear() => _messages.Clear();
}
