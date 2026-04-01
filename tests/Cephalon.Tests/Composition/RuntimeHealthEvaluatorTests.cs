using Cephalon.Engine.Composition;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Composition;

public sealed class RuntimeHealthEvaluatorTests
{
    [Fact]
    public async Task EvaluateReadinessCapturesContributorFailuresAsDependencyReports()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new ThrowingDependencyHealthModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var health = provider.GetRequiredService<RuntimeHealthEvaluator>();

        await runtime.StartAsync(provider);

        var readiness = health.EvaluateReadiness();

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Single(readiness.Dependencies);
        Assert.Contains("failed", readiness.Dependencies[0].Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ThrowingDependencyHealthContributor", readiness.Dependencies[0].Id);
    }
}
