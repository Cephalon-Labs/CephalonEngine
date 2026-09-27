using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class EventPublicationClockTests
{
    [Fact]
    public async Task PublishingDefaultsToTheSystemClock()
    {
        await using var provider = CreateProvider(new RecordingPublisher());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        var before = DateTimeOffset.UtcNow;
        var result = await provider.GetRequiredService<IEventPublicationDispatcher>().PublishAsync(Request("immediate"));
        Assert.InRange(result.AcceptedAtUtc, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task HostClockControlsRelativeAndAbsoluteDeadlinesAndRearmsForEarlierWork()
    {
        var clock = new ControlledTimeProvider();
        var publisher = new RecordingPublisher();
        await using var provider = CreateProvider(publisher, clock);
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
        var dispatcher = provider.GetRequiredService<IEventPublicationDispatcher>();
        var accepted = await dispatcher.PublishAsync(Request("later", "delayMilliseconds", "2000"));
        await dispatcher.PublishAsync(Request("earlier", "scheduledForUtc", clock.GetUtcNow().AddSeconds(1).ToString("O", CultureInfo.InvariantCulture)));
        Assert.Equal(clock.GetUtcNow(), accepted.AcceptedAtUtc);
        Assert.Equal(clock.GetUtcNow(), provider.GetRequiredService<IEventPublicationRuntimeCatalog>().GetByPublicationId("later")!.LastObservedAtUtc);
        Assert.Equal(clock.GetUtcNow().AddSeconds(2).ToString("O", CultureInfo.InvariantCulture), accepted.Metadata["scheduledForUtc"]);

        clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Empty(publisher.Publications);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal("earlier", Assert.Single(publisher.Publications).Id);
        Assert.Equal(clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture), publisher.Publications[0].Metadata["scheduleDispatchedAtUtc"]);
        clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Single(publisher.Publications);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(["earlier", "later"], publisher.Publications.Select(publication => publication.Id));
        Assert.Equal(0, clock.ActiveTimerCount);
        clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(2, publisher.Publications.Count);
    }

    [Fact]
    public async Task PastDueWorkUsesTheHostClockAndPublishesImmediately()
    {
        var clock = new ControlledTimeProvider();
        var publisher = new RecordingPublisher();
        await using var provider = CreateProvider(publisher, clock);
        var result = await provider.GetRequiredService<IEventPublicationDispatcher>().PublishAsync(
            Request("past", "scheduledForUtc", clock.GetUtcNow().AddSeconds(-1).ToString("O", CultureInfo.InvariantCulture)));
        Assert.Equal("past-due-immediate", result.Metadata["scheduleState"]);
        Assert.Equal("past", Assert.Single(publisher.Publications).Id);
        Assert.Equal(0, clock.ActiveTimerCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposalCancelsPendingClockTimers(bool asyncDisposal)
    {
        var clock = new ControlledTimeProvider();
        var publisher = new RecordingPublisher();
        var provider = CreateProvider(publisher, clock);
        try
        {
            await provider.GetRequiredService<IEventPublicationDispatcher>().PublishAsync(Request("pending", "delayMilliseconds", "1000"));
            Assert.Equal(1, clock.ActiveTimerCount);
        }
        finally
        {
            if (asyncDisposal) { await provider.DisposeAsync(); }
            else { provider.Dispose(); }
        }

        Assert.Equal(0, clock.ActiveTimerCount);
        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Empty(publisher.Publications);
    }

    [Fact]
    public async Task ScheduledFailureRetainsTheHostObservationTime()
    {
        var clock = new ControlledTimeProvider();
        await using var provider = CreateProvider(new RecordingPublisher { Fail = true }, clock);
        await provider.GetRequiredService<IEventPublicationDispatcher>().PublishAsync(Request("failure", "delayMilliseconds", "1000"));
        clock.Advance(TimeSpan.FromSeconds(1));
        var state = Assert.Single(provider.GetRequiredService<IEventPublicationRuntimeCatalog>().States);
        Assert.Equal(EventPublicationRuntimeOutcomes.Failed, state.LastOutcome);
        Assert.Equal(clock.GetUtcNow(), state.LastObservedAtUtc);
        Assert.Equal("dispatch-failed", state.Metadata["scheduleState"]);
        Assert.Equal(0, clock.ActiveTimerCount);
    }

    private static ServiceProvider CreateProvider(RecordingPublisher publisher, TimeProvider? clock = null)
    {
        var services = new ServiceCollection();
        if (clock is not null) { services.AddSingleton(clock); }
        services.AddSingleton<IEventPublisher>(publisher);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice", technologies: ["EventDrivenIntegration"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.EnablePublicationScheduling = true;
            });
        });
        return services.BuildServiceProvider();
    }

    private static EventPublicationRequest Request(string id, string? key = null, string? value = null) =>
        new("audit", "audit.created", "{}", id: id,
            metadata: key is null ? null : new Dictionary<string, string> { [key] = value! });

    // Synchronous completion keeps these queue-boundary assertions independent of thread-pool dispatch.
    // The hosting theory separately covers an asynchronous handler-to-terminal-report boundary.
    private sealed class RecordingPublisher : IEventPublisher
    {
        public List<EventPublication> Publications { get; } = [];
        public bool Fail { get; init; }

        public ValueTask PublishAsync(EventPublication publication, CancellationToken cancellationToken = default)
        {
            if (Fail) { throw new InvalidOperationException("Synthetic publication failure."); }
            Publications.Add(publication);
            return ValueTask.CompletedTask;
        }
    }
}
