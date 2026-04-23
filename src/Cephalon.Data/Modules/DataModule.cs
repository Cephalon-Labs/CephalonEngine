using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Configuration;
using Cephalon.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Data.Modules;

internal sealed class DataModule(DataRuntimeOptions options) : ModuleBase, IExecutionGraphContributor, IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "data-runtime",
        displayName: "Data Runtime",
        description: "Runtime-neutral command and query dispatching for Cephalon data workloads.",
        tags: ["data", "cqrs", "runtime"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "data-runtime"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);
        if (options.RegisterReadStore)
        {
            services.TryAddScoped<Abstractions.Data.IReadStore, HandlerDispatchingReadStore>();
        }

        if (options.RegisterWriteStore)
        {
            services.TryAddScoped<Abstractions.Data.IWriteStore, HandlerDispatchingWriteStore>();
        }

        services.TryAddSingleton<CdcCaptureExecutionRuntimeDescriptorCatalog>();
        services.AddSingleton<CdcCaptureExecutionBoundCatalog>();
        services.AddSingleton<Abstractions.Data.ICdcCaptureCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<CdcCaptureExecutionBoundCatalog>());

        services.TryAddSingleton<CdcCaptureRuntimeStateCatalog>();
        services.TryAddSingleton<Abstractions.Data.ICdcCaptureRuntimeStateCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<CdcCaptureRuntimeStateCatalog>());
        services.TryAddSingleton<ICdcCaptureRuntimeReporter>(static serviceProvider =>
            serviceProvider.GetRequiredService<CdcCaptureRuntimeStateCatalog>());
        if (options.EnableExternalCdcRuntimeReporting)
        {
            services.TryAddSingleton<Abstractions.Data.ICdcCaptureExecutionRuntimeReportSink>(static serviceProvider =>
                serviceProvider.GetRequiredService<CdcCaptureRuntimeStateCatalog>());
        }

        services.TryAddSingleton<ManagedConnectorCommandExecutionHistoryStore>();
        services.TryAddSingleton<CdcCaptureExecutionRuntimeCatalog>();
        services.TryAddSingleton<Abstractions.Data.ICdcCaptureExecutionRuntimeCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<CdcCaptureExecutionRuntimeCatalog>());
        services.TryAddSingleton<ManagedConnectorCommandExecutor>();
        services.TryAddSingleton<Abstractions.Data.ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>(static serviceProvider =>
            serviceProvider.GetRequiredService<ManagedConnectorCommandExecutor>());
        services.TryAddSingleton<ManagedConnectorAutomaticRetryHostedService>();

        if (options.CdcExecutionRuntimes.Count > 0)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, ConfiguredCdcCaptureExecutionRuntimeContributor>());
        }

        if (options.EnableCdcExecution)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, SharedCdcCaptureExecutionRuntimeContributor>());
            services.AddHostedService<CdcCaptureHostedService>();
        }

        if (options.EnableManagedConnectorAutomaticRetryExecution)
        {
            services.AddHostedService(static serviceProvider =>
                serviceProvider.GetRequiredService<ManagedConnectorAutomaticRetryHostedService>());
        }
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        if (options.RegisterReadStore)
        {
            capabilities.Add(new Capability(
                key: "data.read",
                displayName: "Data Read Store",
                description: "Executes query handlers through the active Cephalon data runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data"
                }));
        }

        if (options.RegisterWriteStore)
        {
            capabilities.Add(new Capability(
                key: "data.write",
                displayName: "Data Write Store",
                description: "Executes command handlers through the active Cephalon data runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data"
                }));
        }

        if (options.EnableCdcExecution)
        {
            capabilities.Add(new Capability(
                key: "data.cdc.execution",
                displayName: "Shared CDC Execution Pump",
                description: "Runs the shared Cephalon.Data hosted execution pump for active CDC capture implementations.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data",
                    ["executionRuntimeId"] = DataRuntimeIds.CdcExecutionRuntimeId,
                    ["hostedExecutionId"] = DataRuntimeIds.CdcHostedExecutionId,
                    ["executionGraphId"] = DataRuntimeIds.CdcExecutionGraphId
                }));
        }
    }

    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        ArgumentNullException.ThrowIfNull(graphs);

        if (!options.EnableCdcExecution)
        {
            return;
        }

        graphs.Add(new ExecutionGraphDescriptor(
            id: DataRuntimeIds.CdcExecutionGraphId,
            displayName: "Shared CDC Capture Flow",
            description: "Resolves active CDC capture implementations, reads one bounded capture batch, stages publications through the linked outbox, optionally acknowledges durable provider progress, and reports runtime observations.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "resolve-cdc-captures",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "resolve-cdc-captures",
                    displayName: "Resolve CDC Captures",
                    description: "Resolves active CDC capture implementations and their linked outbox bindings from the shared runtime services.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.execution",
                    tags: ["data", "cdc", "runtime"]),
                new ExecutionGraphNodeDescriptor(
                    id: "capture-source-changes",
                    displayName: "Capture Source Changes",
                    description: "Executes one bounded capture batch for each active CDC implementation.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.execution",
                    tags: ["data", "cdc", "capture"]),
                new ExecutionGraphNodeDescriptor(
                    id: "stage-outbox-publications",
                    displayName: "Stage Outbox Publications",
                    description: "Stages captured publications through the matching outbox implementation owned by the active runtime.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.execution",
                    tags: ["data", "cdc", "outbox"]),
                new ExecutionGraphNodeDescriptor(
                    id: "acknowledge-cdc-progress",
                    displayName: "Acknowledge CDC Progress",
                    description: "Lets acknowledgement-capable captures commit provider-facing progress only after the shared runtime staged the linked outbox publications successfully.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.execution",
                    tags: ["data", "cdc", "acknowledgement"]),
                new ExecutionGraphNodeDescriptor(
                    id: "report-cdc-runtime-observation",
                    displayName: "Report CDC Runtime Observation",
                    description: "Projects the latest capture outcome back into the shared CDC runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.execution",
                    tags: ["data", "cdc", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "resolve-cdc-captures",
                    toNodeId: "capture-source-changes",
                    displayName: "resolved"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "capture-source-changes",
                    toNodeId: "stage-outbox-publications",
                    displayName: "captured"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "stage-outbox-publications",
                    toNodeId: "acknowledge-cdc-progress",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "acknowledge-cdc-progress",
                    toNodeId: "report-cdc-runtime-observation",
                    displayName: "reported")
            ],
            tags: ["data", "cdc", "runtime"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "shared-cdc-execution"
            }));
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        ArgumentNullException.ThrowIfNull(hostedExecutions);

        if (!options.EnableCdcExecution)
        {
            return;
        }

        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: DataRuntimeIds.CdcHostedExecutionId,
            displayName: "Shared CDC Capture Pump",
            description: "Runs the shared Cephalon.Data background pump for active CDC capture implementations.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: DataRuntimeIds.CdcExecutionGraphId,
            startsWithHost: true,
            tags: ["data", "cdc", "runtime"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "shared-cdc-execution"
            }));
    }
}
