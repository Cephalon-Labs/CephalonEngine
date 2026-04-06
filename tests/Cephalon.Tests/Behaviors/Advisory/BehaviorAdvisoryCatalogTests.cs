using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors.Advisory;

public sealed class BehaviorAdvisoryCatalogTests
{
    [Fact]
    public void EmptyContributors_ReturnsEmptyList()
    {
        var catalog = new BehaviorAdvisoryCatalog(Enumerable.Empty<IBehaviorAdvisoryContributor>());

        Assert.Empty(catalog.All);
    }

    [Fact]
    public void GetByBehavior_FiltersCorrectly()
    {
        var advisoryA = new StubAdvisory("adv-1", "alpha", BehaviorAdvisorySeverity.Info);
        var advisoryB = new StubAdvisory("adv-2", "beta", BehaviorAdvisorySeverity.Warning);

        var contributor = new StubAdvisoryContributor([advisoryA, advisoryB]);
        var catalog = new BehaviorAdvisoryCatalog([contributor]);

        var result = catalog.GetByBehavior("alpha");

        Assert.Single(result);
        Assert.Equal("adv-1", result[0].Id);
    }

    [Fact]
    public void GetBySeverity_FiltersCorrectly()
    {
        var info = new StubAdvisory("adv-info", "b1", BehaviorAdvisorySeverity.Info);
        var warning = new StubAdvisory("adv-warn", "b2", BehaviorAdvisorySeverity.Warning);
        var critical = new StubAdvisory("adv-crit", "b3", BehaviorAdvisorySeverity.Critical);

        var catalog = new BehaviorAdvisoryCatalog([new StubAdvisoryContributor([info, warning, critical])]);

        var result = catalog.GetBySeverity(BehaviorAdvisorySeverity.Warning);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Id == "adv-warn");
        Assert.Contains(result, a => a.Id == "adv-crit");
    }

    private sealed class StubAdvisory(string id, string behaviorId, BehaviorAdvisorySeverity severity) : IBehaviorAdvisory
    {
        public string Id { get; } = id;
        public string DisplayName { get; } = id;
        public string Description { get; } = id;
        public BehaviorAdvisorySeverity Severity { get; } = severity;
        public string? BehaviorId { get; } = behaviorId;
    }

    private sealed class StubAdvisoryContributor(IReadOnlyList<IBehaviorAdvisory> advisories) : IBehaviorAdvisoryContributor
    {
        public IReadOnlyList<IBehaviorAdvisory> Contribute() => advisories;
    }
}
