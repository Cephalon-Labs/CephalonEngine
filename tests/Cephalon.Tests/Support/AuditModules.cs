using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Audit.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class AuditCaptureModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "audit-capture-tests",
        displayName: "Audit Capture Tests",
        description: "Provides capture writers and ambient actors for Cephalon.Audit tests.",
        tags: ["audit", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CaptureAuditWriter>();
        services.AddSingleton<IAuditWriter>(serviceProvider =>
            serviceProvider.GetRequiredService<CaptureAuditWriter>());
        services.AddSingleton<IAuditActorAccessor>(_ =>
            new FixedAuditActorAccessor(new AuditActor(
                actorId: "user-007",
                displayName: "Avery",
                actorType: "user")));
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed class CaptureAuditWriter : IAuditWriter
{
    private readonly List<AuditEntry> entries = [];

    public IReadOnlyList<AuditEntry> Entries => entries;

    public ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        cancellationToken.ThrowIfCancellationRequested();

        entries.Add(entry);
        return ValueTask.CompletedTask;
    }
}

internal sealed class FixedAuditActorAccessor(AuditActor actor) : IAuditActorAccessor
{
    public AuditActor? Current { get; } = actor;
}
