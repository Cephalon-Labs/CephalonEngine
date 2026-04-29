using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Agentics.Configuration;
using Cephalon.Agentics.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;

namespace Cephalon.Agentics.Modules;

internal sealed class AgenticsModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "agentics-runtime",
        displayName: "Agentics Runtime",
        description: "Companion runtime services for agentic workloads.",
        tags: ["technology", "agentics"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "technology-pack",
            ["technology"] = "agentic-workloads"
        });

    private readonly AgenticRuntimeOptions options;
    private bool hasToolContributors;

    public AgenticsModule(AgenticRuntimeOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("agentic-workloads"))
        {
            return;
        }

        hasToolContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IAgentToolContributor));
        services.TryAddSingleton(options);
        services.TryAddSingleton<IAgentToolCatalog, AgentToolCatalog>();
        if (options.EnableExecution)
        {
            services.TryAddSingleton<AgentToolRunCatalog>();
            services.TryAddSingleton<IAgentToolRunCatalog>(static serviceProvider =>
                serviceProvider.GetRequiredService<AgentToolRunCatalog>());
            services.TryAddSingleton<IAgentToolRunReporter>(static serviceProvider =>
                serviceProvider.GetRequiredService<AgentToolRunCatalog>());
            services.TryAddSingleton<IAgentToolDispatcher, AgentToolDispatcher>();
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, AgenticsRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("agentic-workloads"))
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "agentics.runtime",
            displayName: "Agentic Runtime",
            description: "Coordinates agentic runtime services when the agentic workload profile is active."));

        if (options.EnableExecution)
        {
            capabilities.Add(new Capability(
                key: "agentics.execution",
                displayName: "Agent Execution",
                description: "Supports tool execution and agent action dispatch.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "agentic-workloads",
                    ["executionOwnership"] = "cephalon-managed",
                    ["runtime"] = "agent-tool-dispatcher",
                    ["retryPolicy"] = options.ExecutionMaxAttempts > 1 ? "bounded-in-process" : "none",
                    ["retryMaxAttempts"] = Math.Max(1, options.ExecutionMaxAttempts).ToString(CultureInfo.InvariantCulture),
                    ["retryDelayMilliseconds"] = Math.Max(0, options.ExecutionRetryDelayMilliseconds).ToString(CultureInfo.InvariantCulture),
                    ["retryDurability"] = "none",
                    ["retryScope"] = options.ExecutionMaxAttempts > 1 ? "process-local" : "none",
                    ["idempotencyPolicy"] = options.EnableExecutionIdempotency ? "completed-run" : "none",
                    ["idempotencyKey"] = options.EnableExecutionIdempotency ? "tool-run" : "none",
                    ["idempotencyRetentionMinutes"] = Math.Max(1, options.ExecutionIdempotencyRetentionMinutes).ToString(CultureInfo.InvariantCulture),
                    ["idempotencyDurability"] = "none",
                    ["idempotencyScope"] = options.EnableExecutionIdempotency ? "process-local" : "none"
                }));
        }

        if (options.EnableMemory)
        {
            capabilities.Add(new Capability(
                key: "agentics.memory",
                displayName: "Agent Memory",
                description: "Supports stateful memory and context persistence for agentic flows.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "agentic-workloads"
                }));
        }

        if (options.Tools.Count > 0 || hasToolContributors)
        {
            capabilities.Add(new Capability(
                key: "agentics.tools",
                displayName: "Agent Tools",
                description: "Exposes registered agent tool descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "agentic-workloads",
                    ["toolCount"] = options.Tools.Count.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }
}
