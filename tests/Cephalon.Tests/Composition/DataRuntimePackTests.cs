using Cephalon.Abstractions.Data;
using Cephalon.Data.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DataRuntimePackTests
{
    [Fact]
    public async Task AddDataDispatchesCommandsAndQueriesThroughRegisteredHandlers()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DataDispatchingTestModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();

        await writeStore.ExecuteAsync(new ActivateTenantCommand("tenant-001"));
        var orderId = await writeStore.ExecuteAsync(new CreateOrderCommand("Ada"));
        var snapshot = await readStore.ExecuteAsync(new GetDispatchingSnapshotQuery());

        Assert.Equal("order-002", orderId);
        Assert.Equal(1, snapshot.ActivatedTenants);
        Assert.Equal("order-002", snapshot.LastCreatedOrderId);
        Assert.Equal("Ada", snapshot.LastCustomerName);
    }

    [Fact]
    public void AddDataRegistersReadAndWriteCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.read");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.write");
    }

    [Fact]
    public async Task AddDataThrowsHelpfulErrorWhenCommandHandlerIsMissing()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await writeStore.ExecuteAsync(new MissingCommand("missing")));

        Assert.Contains(nameof(MissingCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ICommandHandler<MissingCommand>), exception.Message, StringComparison.Ordinal);
    }
}
