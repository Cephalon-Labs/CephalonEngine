using System.Reflection;
using Cephalon.Behaviors.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Behaviors.Diagnostics;

public sealed class BehaviorDiagnosticsTests
{
    private static List<EventId> AllEventIds()
    {
        return typeof(BehaviorDiagnostics)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(EventId))
            .Select(f => (EventId)f.GetValue(null)!)
            .ToList();
    }

    [Fact]
    public void AllEventIds_AreInExpectedRange()
    {
        var ids = AllEventIds();

        Assert.NotEmpty(ids);
        Assert.All(ids, id => Assert.InRange(id.Id, 5100, 5109));
    }

    [Fact]
    public void AllEventIds_HaveNonEmptyNames()
    {
        var ids = AllEventIds();

        Assert.NotEmpty(ids);
        Assert.All(ids, id =>
        {
            Assert.False(string.IsNullOrEmpty(id.Name), $"EventId {id.Id} has a null or empty name.");
        });
    }
}
