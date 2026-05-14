using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Agentics.Registration;
using Cephalon.Agentics.Services;
using Cephalon.Data.Registration;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class AgenticsDurableIdempotencyTests
{
    [Fact]
    public async Task AddAgenticsFailsDurableIdempotencyWhenInboxProviderIsMissing()
    {
        var services = CreateDurableAgenticsServices();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
                toolId: "durable-analyst",
                runId: "missing-inbox-run",
                actorId: "operator",
                correlationId: "corr-missing-inbox")));

        Assert.Contains("requires exactly one active IInbox provider", exception.Message);
        Assert.Contains("none are registered", exception.Message);
    }

    [Fact]
    public async Task AddAgenticsFailsDurableIdempotencyWhenMultipleInboxProvidersAreRegistered()
    {
        var services = CreateDurableAgenticsServices();
        services.AddSingleton<IInbox, TestInbox>();
        services.AddSingleton<IInbox, TestInbox>();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
                toolId: "durable-analyst",
                runId: "ambiguous-inbox-run",
                actorId: "operator",
                correlationId: "corr-ambiguous-inbox")));

        Assert.Contains("requires exactly one active IInbox provider", exception.Message);
        Assert.Contains("multiple inbox providers are registered", exception.Message);
    }

    [Fact]
    public async Task AddAgenticsCanUseActiveInboxForDurableDuplicateCompletedRunSuppression()
    {
        var databaseName = $"agentics-durable-idempotency-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddSingleton<DurableAgentToolExecutor>();
        services.AddSingleton<IAgentToolExecutor>(static serviceProvider =>
            serviceProvider.GetRequiredService<DurableAgentToolExecutor>());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["AgenticWorkloads"],
                data: new DataSettings(provider: "EntityFramework")));
            engine.AddData();
            engine.AddAgentics(options =>
            {
                options.EnableExecutionIdempotency = true;
                options.ExecutionIdempotencyDurability = "inbox";
                options.Tools.Add(new AgentToolDescriptor(
                    id: "durable-analyst",
                    displayName: "Durable Analyst",
                    description: "Exercises durable inbox-backed idempotency for managed agentics execution."));
            });
            engine.AddEntityFrameworkData<InboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterInbox = true);
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();
        var executor = provider.GetRequiredService<DurableAgentToolExecutor>();
        var inbox = provider.GetRequiredService<IInbox>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var first = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "durable-analyst",
            runId: "durable-agent-run-001",
            actorId: "operator",
            correlationId: "corr-agentics-durable-001"));
        var duplicate = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "durable-analyst",
            runId: "durable-agent-run-001",
            actorId: "operator",
            correlationId: "corr-agentics-durable-001"));

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InboxCatalogDbContext>();
        var inboxEntry = await dbContext.InboxMessages.SingleAsync();
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var entry = Assert.Single(agenticsSurface.Entries, surfaceEntry => surfaceEntry.Id == "durable-analyst");
        var runCatalog = provider.GetRequiredService<IAgentToolRunCatalog>();
        var runState = Assert.Single(runCatalog.GetByToolId("durable-analyst"));

        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, first.Outcome);
        Assert.Equal(AgentToolExecutionOutcomes.Skipped, duplicate.Outcome);
        Assert.Equal(1, executor.CallCount);
        Assert.True(await inbox.HasProcessedAsync("agentics:durable-analyst:durable-agent-run-001"));
        Assert.Equal("agentics:durable-analyst:durable-agent-run-001", inboxEntry.Id);
        Assert.Equal("agentics.tool-runs", inboxEntry.ChannelId);
        Assert.Equal("agentics.tool-run.completed", inboxEntry.MessageType);
        Assert.Equal("inbox", first.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-inbox", first.Metadata["idempotencyScope"]);
        Assert.Equal("completed-marked", first.Metadata["idempotencyOutcome"]);
        Assert.Equal("IInbox", first.Metadata["idempotencyStore"]);
        Assert.Equal("inbox", duplicate.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-inbox", duplicate.Metadata["idempotencyScope"]);
        Assert.Equal("duplicate-skipped", duplicate.Metadata["idempotencyOutcome"]);
        Assert.Equal("agentics:durable-analyst:durable-agent-run-001", duplicate.Metadata["completedInboxMessageId"]);
        Assert.Equal(AgentToolExecutionOutcomes.Skipped, runState.LastOutcome);
        Assert.True(runState.DuplicateCompleted);
        Assert.Equal("inbox", entry.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-inbox", entry.Metadata["idempotencyScope"]);
        Assert.Equal("true", entry.Metadata["idempotencyInboxConfigured"]);
        Assert.Equal("1", entry.Metadata["idempotencyInboxCount"]);
        Assert.Equal("active-inbox", entry.Metadata["idempotencyProviderInterop"]);
        Assert.Equal("entity-framework-inbox", entry.Metadata["idempotencyInboxId"]);
        Assert.Equal("entity-framework", entry.Metadata["idempotencyInboxProvider"]);
        Assert.Equal("processed-message-table", entry.Metadata["idempotencyInboxMode"]);
    }

    private static ServiceCollection CreateDurableAgenticsServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DurableAgentToolExecutor>();
        services.AddSingleton<IAgentToolExecutor>(static serviceProvider =>
            serviceProvider.GetRequiredService<DurableAgentToolExecutor>());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics(options =>
            {
                options.EnableExecutionIdempotency = true;
                options.ExecutionIdempotencyDurability = "inbox";
                options.Tools.Add(new AgentToolDescriptor(
                    id: "durable-analyst",
                    displayName: "Durable Analyst",
                    description: "Exercises durable inbox-backed idempotency for managed agentics execution."));
            });
        });

        return services;
    }

    private sealed class DurableAgentToolExecutor : IAgentToolExecutor
    {
        public string ToolId => "durable-analyst";

        public int CallCount { get; private set; }

        public ValueTask<AgentToolExecutionResult> ExecuteAsync(
            AgentToolExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return ValueTask.FromResult(AgentToolExecutionResult.Succeeded(
                $"Durable run {context.RunId} completed.",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["executor"] = nameof(DurableAgentToolExecutor)
                }));
        }
    }

    private sealed class TestInbox : IInbox
    {
        public ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
