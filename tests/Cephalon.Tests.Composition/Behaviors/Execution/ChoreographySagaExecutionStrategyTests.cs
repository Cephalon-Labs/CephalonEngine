using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Publishers;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="ChoreographySagaExecutionStrategy"/>.</summary>
public sealed class ChoreographySagaExecutionStrategyTests
{
    [AppBehavior("saga.choreography.single")]
    private sealed class SinglePublicationBehavior : IAppBehavior<string, SagaChoreographyPublication>
    {
        public Task<SagaChoreographyPublication> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SagaChoreographyPublication(
                id: "publication-1",
                channelId: "orders.events",
                eventType: "order-submitted",
                payload: $"{{\"value\":\"{input}\"}}",
                occurredAtUtc: DateTimeOffset.UtcNow));
        }
    }

    [AppBehavior("saga.choreography.combined")]
    private sealed class CombinedResultBehavior : IAppBehavior<string, SagaChoreographyStepResult>
    {
        public Task<SagaChoreographyStepResult> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SagaChoreographyStepResult(
                output: $"accepted:{input}",
                publications:
                [
                    new SagaChoreographyPublication(
                        id: "publication-2",
                        channelId: "payments.events",
                        eventType: "payment-requested",
                        payload: $"{{\"value\":\"{input}\"}}",
                        occurredAtUtc: DateTimeOffset.UtcNow,
                        isCompensation: true)
                ]));
        }
    }

    [AppBehavior("saga.choreography.local-only")]
    private sealed class LocalOnlyBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"local:{input}");
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input, IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "saga-choreography", ["in-memory"]);
        var slot = BehaviorExecutionSlot.ForType(typeof(TBehavior));
        return new BehaviorExecutionContext
        {
            Descriptor = descriptor,
            BehaviorInstance = behavior,
            Slot = slot,
            Input = input,
            BehaviorContext = behaviorContext
        };
    }

    private static ChoreographySagaExecutionStrategy MakeStrategy(ISagaChoreographyPublisher publisher)
        => new(publisher, NullLogger<ChoreographySagaExecutionStrategy>.Instance);

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_SinglePublication_PublishesAndReturnsAccepted()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var behaviorContext = new TestBehaviorContext(
            "saga.choreography.single",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TenantId"] = "tenant-a"
            },
            correlationId: "corr-123");
        var executionContext = MakeContext(new SinglePublicationBehavior(), "value", behaviorContext);

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.False(result.IsFireAndForget);
        Assert.IsType<SagaChoreographyPublication>(result.Output);
        var published = Assert.Single(publisher.PublishedPublications);
        Assert.Equal("corr-123", published.CorrelationId);
        Assert.Equal("tenant-a", published.TenantId);
    }

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_CombinedResult_PublishesAndPreservesLocalOutput()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var executionContext = MakeContext(
            new CombinedResultBehavior(),
            "checkout",
            new TestBehaviorContext("saga.choreography.combined", correlationId: "corr-456"));

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Equal("accepted:checkout", result.Output);
        var published = Assert.Single(publisher.PublishedPublications);
        Assert.True(published.IsCompensation);
        Assert.Equal("corr-456", published.CorrelationId);
    }

    [Fact]
    public async Task ChoreographySagaExecutionStrategy_LocalOnlyOutput_Returns200WithoutPublication()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = MakeStrategy(publisher);
        var executionContext = MakeContext(
            new LocalOnlyBehavior(),
            "checkout",
            new TestBehaviorContext("saga.choreography.local-only", correlationId: "corr-789"));

        var result = await strategy.ExecuteAsync(executionContext);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal("local:checkout", result.Output);
        Assert.Empty(publisher.PublishedPublications);
    }

    [Fact]
    public void ChoreographySagaExecutionStrategy_Pattern_IsSagaChoreographyLiteral()
    {
        var strategy = MakeStrategy(new InMemorySagaChoreographyPublisher());

        Assert.Equal("saga-choreography", strategy.Pattern);
    }
}
