using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Tests.Composition;

public sealed class EngineSettingsTests
{
    [Fact]
    public void FromConfigurationReadsStructuredPhase8Settings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Data:Provider"] = "EntityFramework",
                ["Engine:Data:ReadWriteSplit"] = "true",
                ["Engine:Data:Outbox:Enabled"] = "true",
                ["Engine:Data:Ids:Generator"] = "Sfid",
                ["Engine:Identity:Enabled"] = "true",
                ["Engine:Identity:AuthorizationModes:0"] = "RBAC",
                ["Engine:Identity:AuthorizationModes:1"] = "Policy",
                ["Engine:Tenancy:Enabled"] = "true",
                ["Engine:Tenancy:Mode"] = "SharedDatabase",
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Messaging:Provider"] = "Wolverine"
            })
            .Build();

        var settings = EngineSettings.FromConfiguration(configuration);

        Assert.True(settings.HasValues);
        Assert.Equal("EntityFramework", settings.Data.Provider);
        Assert.True(settings.Data.ReadWriteSplit);
        Assert.True(settings.Data.OutboxEnabled);
        Assert.Equal("Sfid", settings.Data.IdGenerator);
        Assert.True(settings.Identity.Enabled);
        Assert.Equal(["RBAC", "Policy"], settings.Identity.AuthorizationModes);
        Assert.True(settings.Tenancy.Enabled);
        Assert.Equal("SharedDatabase", settings.Tenancy.Mode);
        Assert.True(settings.Audit.Enabled);
        Assert.Equal("Wolverine", settings.Messaging.Provider);
    }
}
