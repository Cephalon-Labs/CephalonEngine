using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Agentics.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Demonstrates an adoption-quality agentics module with a tool descriptor, executor, and policy hook.
/// </summary>
public sealed class ShowcaseAgenticsModule : ModuleBase, IAgentToolContributor, IAgentToolExecutor, IAgentToolExecutionPolicy, IAgentToolExecutionObserver
{
    private const string ToolIdValue = "showcase.catalog-inspector";
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.agentics",
        displayName: "Showcase Agentics",
        description: "Agentic tool contribution module for the showcase sample.",
        tags: ["showcase", "agentics", "tools"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentToolContributor>(this);
        services.AddSingleton<IAgentToolExecutor>(this);
        services.AddSingleton<IAgentToolExecutionPolicy>(this);
        services.AddSingleton<IAgentToolExecutionObserver>(this);
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.agentics.catalog-inspection",
            displayName: "Showcase catalog inspection",
            description: "Allows the showcase agentic tool to inspect catalog posture through a Cephalon-managed run."));
    }

    /// <inheritdoc />
    public void RegisterTools(IAgentToolRegistry tools)
    {
        tools.Add(new AgentToolDescriptor(
            id: ToolIdValue,
            displayName: "Showcase Catalog Inspector",
            description: "Inspects catalog posture through the Cephalon-managed agentics dispatcher.",
            tags: ["showcase", "catalog", "operator"],
            capabilityKeys: ["showcase.agentics.catalog-inspection"],
            metadata: new Dictionary<string, string>
            {
                ["sample"] = "showcase",
                ["runtime"] = "cephalon-managed"
            }));
    }

    /// <inheritdoc />
    public string ToolId => ToolIdValue;

    /// <inheritdoc />
    public ValueTask<AgentToolExecutionDecision> EvaluateAsync(
        AgentToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Metadata.TryGetValue("requiresApproval", out var requiresApproval) &&
            string.Equals(requiresApproval, "true", StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(AgentToolExecutionDecision.RequireApproval(
                "Showcase catalog inspection was held for operator approval.",
                new Dictionary<string, string>
                {
                    ["policy"] = "showcase-agentics"
                }));
        }

        return ValueTask.FromResult(AgentToolExecutionDecision.Allow(
            metadata: new Dictionary<string, string>
            {
                ["policy"] = "showcase-agentics"
            }));
    }

    /// <inheritdoc />
    public ValueTask<AgentToolExecutionResult> ExecuteAsync(
        AgentToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var focus = context.Arguments.TryGetValue("focus", out var configuredFocus) &&
            !string.IsNullOrWhiteSpace(configuredFocus)
            ? configuredFocus
            : "catalog";

        return ValueTask.FromResult(AgentToolExecutionResult.Succeeded(
            outputSummary: $"Inspected showcase {focus} posture.",
            metadata: new Dictionary<string, string>
            {
                ["sample"] = "showcase",
                ["focus"] = focus
            }));
    }

    /// <inheritdoc />
    public ValueTask ObserveAsync(
        AgentToolExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}
