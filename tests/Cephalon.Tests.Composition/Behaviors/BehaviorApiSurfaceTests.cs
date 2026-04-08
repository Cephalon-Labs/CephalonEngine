using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Builders;

namespace Cephalon.Tests.Behaviors;

public sealed class BehaviorApiSurfaceTests
{
    [Fact]
    public void CreateDefaultSplitsTwoSegmentBehaviorIdIntoGroupAndOperation()
    {
        var descriptor = BehaviorApiSurfaceDescriptor.CreateDefault("cart.get");

        Assert.Equal("cart", descriptor.GroupPath);
        Assert.Equal("get", descriptor.OperationPath);
    }

    [Fact]
    public void CreateDefaultJoinsAllButFinalSegmentIntoGroupPath()
    {
        var descriptor = BehaviorApiSurfaceDescriptor.CreateDefault("catalog.items.lookup");

        Assert.Equal("catalog/items", descriptor.GroupPath);
        Assert.Equal("lookup", descriptor.OperationPath);
    }

    [Fact]
    public void CreateDefaultKeepsSingleSegmentBehaviorIdAsOperationOnly()
    {
        var descriptor = BehaviorApiSurfaceDescriptor.CreateDefault("status");

        Assert.Equal(string.Empty, descriptor.GroupPath);
        Assert.Equal("status", descriptor.OperationPath);
    }

    [Fact]
    public void TopologyBuilderWithApiSurfaceOverridesDefaultDerivedSurface()
    {
        var descriptor = new BehaviorTopologyBuilder()
            .AsCqrs()
            .ViaHttpRest()
            .ViaHttpJsonRpc()
            .WithApiSurface("catalog/items", "lookup")
            .Build("catalog.lookup");

        Assert.Equal("catalog/items", descriptor.ApiSurface.GroupPath);
        Assert.Equal("lookup", descriptor.ApiSurface.OperationPath);
        Assert.Contains("http.rest", descriptor.TransportIds);
        Assert.Contains("http.jsonrpc", descriptor.TransportIds);
    }
}
