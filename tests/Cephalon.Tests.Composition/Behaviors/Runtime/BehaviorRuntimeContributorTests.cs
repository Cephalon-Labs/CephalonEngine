using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Runtime;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors.Runtime;

public sealed class BehaviorRuntimeContributorTests
{
    [Fact]
    public void EmptyCatalog_ReturnsZeroBehaviorCount()
    {
        var catalog = new BehaviorCatalog(Enumerable.Empty<IBehaviorContributor>());
        var contributor = new BehaviorRuntimeContributor(catalog);

        var surface = contributor.DescribeRuntimeSurface();
        var entry = surface.Entries.Single();

        Assert.Equal("0", entry.Metadata["behaviorCount"]);
        Assert.Equal("0", entry.Metadata["featureGatedBehaviorCount"]);
    }

    [Fact]
    public void WithBehaviors_ReturnsCorrectPatternCount()
    {
        var descriptors = new[]
        {
            new BehaviorTopologyDescriptor("b1", "cqrs", ["rest"], requiredFeatureFlagIds: ["host.preview"], sourceModuleId: "tests.runtime"),
            new BehaviorTopologyDescriptor("b2", "cqrs", ["grpc"]),
            new BehaviorTopologyDescriptor("b3", "event-driven", ["kafka"])
        };

        var catalog = new BehaviorCatalog([new StubContributor(descriptors)]);
        var contributor = new BehaviorRuntimeContributor(catalog);

        var surface = contributor.DescribeRuntimeSurface();
        var entry = Assert.Single(surface.Entries, static candidate => candidate.Id == "behaviors-runtime");
        var featureGatedEntry = Assert.Single(surface.Entries, static candidate => candidate.Id == "b1");

        Assert.Equal("3", entry.Metadata["behaviorCount"]);
        Assert.Equal("1", entry.Metadata["featureGatedBehaviorCount"]);
        Assert.Equal("2", entry.Metadata["pattern.cqrs"]);
        Assert.Equal("1", entry.Metadata["pattern.event-driven"]);
        Assert.Equal("tests.runtime", featureGatedEntry.Metadata["sourceModuleId"]);
        Assert.Equal("host.preview", featureGatedEntry.Metadata["requiredFeatureFlagIds"]);
    }

    [Fact]
    public void SurfaceId_IsBehaviors()
    {
        var catalog = new BehaviorCatalog(Enumerable.Empty<IBehaviorContributor>());
        var contributor = new BehaviorRuntimeContributor(catalog);

        var surface = contributor.DescribeRuntimeSurface();

        Assert.Equal("behaviors", surface.SurfaceId);
        Assert.Equal("behaviors", surface.TechnologyId);
    }

    private sealed class StubContributor(IReadOnlyList<BehaviorTopologyDescriptor> descriptors) : IBehaviorContributor
    {
        public IReadOnlyList<BehaviorTopologyDescriptor> Contribute() => descriptors;
    }
}
