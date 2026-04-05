using Cephalon.Abstractions.Ids;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Ids.Sfid.Configuration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SfidNet.Abstractions;

namespace Cephalon.Tests.Composition;

public sealed class SfidIdPackTests
{
    [Fact]
    public async Task AddSfidIdsResolvesOfficialGeneratorThroughIIdGenerator()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                data: new DataSettings(idGenerator: "Sfid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 1;
                options.WorkerId = 7;
                options.WorkerCapacity = 32;
                options.ClockRegressionToleranceMilliseconds = 2;
            });
        });

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<IIdGenerator>();

        var first = await generator.GenerateAsync();
        var second = await generator.GenerateAsync();

        Assert.Equal("sfid", generator.StrategyId);
        Assert.NotEmpty(first);
        Assert.NotEmpty(second);
        Assert.NotEqual(first, second);
        Assert.True(first.All(char.IsDigit));
        Assert.True(second.All(char.IsDigit));
    }

    [Fact]
    public async Task AddSfidIdsReadsGeneratorTopologyFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith",
                [$"{EngineSettings.SectionName}:Data:Ids:Generator"] = "Sfid",
                [$"{SfidIdOptions.DefaultSectionPath}:DatacenterId"] = "3",
                [$"{SfidIdOptions.DefaultSectionPath}:WorkerId"] = "11",
                [$"{SfidIdOptions.DefaultSectionPath}:WorkerCapacity"] = "32",
                [$"{SfidIdOptions.DefaultSectionPath}:ClockRegressionToleranceMilliseconds"] = "5"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(configuration, engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddSfidIds();
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SfidIdOptions>();
        var generator = provider.GetRequiredService<IIdGenerator>();
        var generated = await generator.GenerateAsync();

        Assert.Equal(3, options.DatacenterId);
        Assert.Equal(11, options.WorkerId);
        Assert.Equal(32, options.WorkerCapacity);
        Assert.Equal(5, options.ClockRegressionToleranceMilliseconds);
        Assert.NotEmpty(generated);
    }

    [Fact]
    public void AddSfidIdsRejectsMismatchedConfiguredIdStrategy()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                data: new DataSettings(idGenerator: "Ulid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 1;
                options.WorkerId = 7;
            });
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IIdGenerator>());

        Assert.Contains("Ulid", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Sfid", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSfidIdsAlsoRegistersOfficialSfidGenerator()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                data: new DataSettings(idGenerator: "Sfid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 2;
                options.WorkerId = 9;
            });
        });

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<ISfidGenerator>();

        var first = generator.NextId();
        var second = generator.NextId();

        Assert.True(first > 0);
        Assert.True(second > 0);
        Assert.NotEqual(first, second);
    }
}
