using System.Diagnostics;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Ids;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Tenancy;
using Cephalon.Audit.Registration;
using Cephalon.Audit.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Direct fallback-chain coverage for <see cref="IAuditActorAccessor" /> and
/// <see cref="IAuditRecorder" />. The existing
/// <c>IdentityAspNetCoreAuditActorBridgeTests</c> in <c>Cephalon.Tests.Hosting</c>
/// pin the HTTP-host bridge that promotes <see cref="System.Security.Claims.ClaimsPrincipal" />
/// onto the ambient audit actor, but the lower-level fallback rules in
/// <c>DefaultAuditRecorder</c> (explicit request actor wins → ambient accessor →
/// system actor; explicit correlation id wins → <see cref="Activity.Current"/>
/// trace id; explicit tenant id wins → <see cref="ITenantContextAccessor.Current"/>;
/// explicit entry id wins → <see cref="IIdGenerator"/> → <see cref="Guid"/>) and
/// the <see cref="DefaultAuditActorAccessor"/> always-null contract have only
/// been exercised end-to-end through that hosting integration.
/// </summary>
public sealed class AuditActorAndRecorderFallbackTests
{
    [Fact]
    public async Task DefaultAuditActorAccessor_AlwaysReturnsNull_WhenNoBridgeIsActive()
    {
        await using var provider = BuildProvider();

        var accessor = provider.GetRequiredService<IAuditActorAccessor>();

        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task DefaultAuditRecorder_FallsBackToSystemActor_WhenAccessorReturnsNull()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.True(entry.Actor.IsSystem);
        Assert.Equal("system", entry.Actor.ActorId);
        Assert.Equal("system", entry.Actor.ActorType);
    }

    [Fact]
    public async Task DefaultAuditRecorder_PrefersAmbientAccessorActor_WhenRequestActorIsNull()
    {
        await using var provider = BuildProvider(extraModules: new[] { new FixedAuditActorModule("user-007", "Avery") });
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("user-007", entry.Actor.ActorId);
        Assert.Equal("Avery", entry.Actor.DisplayName);
        Assert.False(entry.Actor.IsSystem);
    }

    [Fact]
    public async Task DefaultAuditRecorder_PrefersExplicitRequestActor_OverAmbientAccessor()
    {
        await using var provider = BuildProvider(extraModules: new[] { new FixedAuditActorModule("user-007", "Avery") });
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest(actor: new AuditActor(
            actorId: "user-explicit",
            displayName: "Explicit Operator",
            actorType: "operator")));

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("user-explicit", entry.Actor.ActorId);
        Assert.Equal("Explicit Operator", entry.Actor.DisplayName);
        Assert.Equal("operator", entry.Actor.ActorType);
    }

    [Fact]
    public async Task DefaultAuditRecorder_FallsBackToActivityCurrentTraceId_WhenCorrelationIdMissing()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activitySource = new ActivitySource("Cephalon.Tests.AuditActorAndRecorderFallbackTests");
        using var activity = activitySource.StartActivity("audit-fallback-test");

        Assert.NotNull(activity);

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.Equal(activity!.TraceId.ToString(), entry.CorrelationId);
    }

    [Fact]
    public async Task DefaultAuditRecorder_PrefersExplicitCorrelationId_OverActivityCurrentTraceId()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activitySource = new ActivitySource("Cephalon.Tests.AuditActorAndRecorderFallbackTests");
        using var activity = activitySource.StartActivity("audit-explicit-correlation-test");

        Assert.NotNull(activity);

        await recorder.RecordAsync(NewRequest(correlationId: "correlation-explicit-001"));

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("correlation-explicit-001", entry.CorrelationId);
    }

    [Fact]
    public async Task DefaultAuditRecorder_LeavesCorrelationIdNull_WhenNoActivityAndNoExplicitId()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.Null(entry.CorrelationId);
    }

    [Fact]
    public async Task DefaultAuditRecorder_GeneratesGuidEntryId_WhenIdGeneratorMissing()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.False(string.IsNullOrWhiteSpace(entry.Id));
        Assert.True(Guid.TryParseExact(entry.Id, "N", out _));
    }

    [Fact]
    public async Task DefaultAuditRecorder_PrefersExplicitEntryId_OverGeneratedFallback()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest(entryId: "audit-entry-explicit-001"));

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("audit-entry-explicit-001", entry.Id);
    }

    [Fact]
    public async Task DefaultAuditRecorder_UsesIdGenerator_WhenRegistered()
    {
        await using var provider = BuildProvider(extraModules: new[] { new StubIdGeneratorModule(strategyId: "test", generatedId: "stub-id-007") });
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("stub-id-007", entry.Id);
    }

    [Fact]
    public async Task DefaultAuditRecorder_FallsBackToTenantContextAccessor_WhenRequestTenantMissing()
    {
        await using var provider = BuildProvider(extraModules: new[]
        {
            new FixedTenantContextModule(new TenantContext(tenantId: "tenant-ambient"))
        });
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest());

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("tenant-ambient", entry.TenantId);
    }

    [Fact]
    public async Task DefaultAuditRecorder_PrefersExplicitTenantId_OverTenantContextAccessor()
    {
        await using var provider = BuildProvider(extraModules: new[]
        {
            new FixedTenantContextModule(new TenantContext(tenantId: "tenant-ambient"))
        });
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();

        await recorder.RecordAsync(NewRequest(tenantId: "tenant-explicit"));

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("tenant-explicit", entry.TenantId);
    }

    [Fact]
    public async Task DefaultAuditRecorder_OccurredAtUtcDefaultsToUtcNow_WhenRequestDoesNotSupplyOne()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();
        var before = DateTimeOffset.UtcNow;

        await recorder.RecordAsync(NewRequest());

        var after = DateTimeOffset.UtcNow;
        var entry = Assert.Single(capture.Entries);
        Assert.InRange(entry.OccurredAtUtc, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public async Task DefaultAuditRecorder_PreservesExplicitOccurredAtUtc_WhenSupplied()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var capture = provider.GetRequiredService<CaptureAuditWriter>();
        var explicitOccurredAt = new DateTimeOffset(2025, 6, 1, 12, 30, 0, TimeSpan.Zero);

        await recorder.RecordAsync(NewRequest(occurredAtUtc: explicitOccurredAt));

        var entry = Assert.Single(capture.Entries);
        Assert.Equal(explicitOccurredAt, entry.OccurredAtUtc);
    }

    [Fact]
    public async Task DefaultAuditRecorder_PropagatesWriterException_AfterAttemptingTheWrite()
    {
        await using var provider = BuildProvider(extraModules: new[] { new ThrowingAuditWriterModule() });
        var recorder = provider.GetRequiredService<IAuditRecorder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            recorder.RecordAsync(NewRequest()).AsTask());
    }

    [Fact]
    public async Task DefaultAuditRecorder_ThrowsOperationCanceled_WhenCancellationRequested()
    {
        await using var provider = BuildProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            recorder.RecordAsync(NewRequest(), cts.Token).AsTask());
    }

    private static AuditRecordRequest NewRequest(
        AuditActor? actor = null,
        string? entryId = null,
        string? correlationId = null,
        string? tenantId = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        return new AuditRecordRequest(
            category: "documents",
            action: "publish",
            summary: "Published a document.",
            subjectType: "document",
            subjectId: "doc-001",
            entryId: entryId,
            occurredAtUtc: occurredAtUtc,
            actor: actor,
            outcome: AuditOutcome.Succeeded,
            tenantId: tenantId,
            correlationId: correlationId);
    }

    private static ServiceProvider BuildProvider(IModule[]? extraModules = null)
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                technologies: ["IdentityAccess"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new AuditCaptureWriterModule());
            if (extraModules is not null)
            {
                foreach (var module in extraModules)
                {
                    engine.AddModule(module);
                }
            }

            engine.AddAudit();
        });

        return services.BuildServiceProvider();
    }

    private sealed class FixedAuditActorModule(string actorId, string? displayName) : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "audit-fixed-actor-tests",
            displayName: "Audit fixed actor tests",
            description: "Registers a fixed-actor IAuditActorAccessor for direct fallback-chain tests.",
            tags: ["audit", "tests"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAuditActorAccessor>(_ =>
                new FixedAuditActorAccessor(new AuditActor(
                    actorId: actorId,
                    displayName: displayName,
                    actorType: "user")));
        }

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }
    }

    private sealed class FixedTenantContextModule(TenantContext tenantContext) : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "audit-fixed-tenant-tests",
            displayName: "Audit fixed tenant tests",
            description: "Registers a fixed-tenant ITenantContextAccessor for direct fallback-chain tests.",
            tags: ["audit", "tenancy", "tests"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITenantContextAccessor>(_ => new FixedTenantContextAccessor(tenantContext));
        }

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }
    }

    private sealed class StubIdGeneratorModule(string strategyId, string generatedId) : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "audit-stub-id-generator-tests",
            displayName: "Audit stub id generator tests",
            description: "Registers a stub IIdGenerator for direct entry-id-fallback tests.",
            tags: ["audit", "ids", "tests"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IIdGenerator>(_ => new StubIdGenerator(strategyId, generatedId));
        }

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }
    }

    private sealed class ThrowingAuditWriterModule : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "audit-throwing-writer-tests",
            displayName: "Audit throwing writer tests",
            description: "Registers an IAuditWriter that always throws for direct exception-propagation tests.",
            tags: ["audit", "tests"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAuditWriter, ThrowingAuditWriter>();
        }

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }
    }

    private sealed class FixedTenantContextAccessor(TenantContext context) : ITenantContextAccessor
    {
        public TenantContext? Current { get; } = context;
    }

    private sealed class StubIdGenerator(string strategyId, string generatedId) : IIdGenerator
    {
        public string StrategyId { get; } = strategyId;

        public ValueTask<string> GenerateAsync(IdGenerationRequest? request = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<string>(generatedId);
        }
    }

    private sealed class ThrowingAuditWriter : IAuditWriter
    {
        public ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Audit writer intentionally fails for fallback-chain tests.");
        }
    }
}
