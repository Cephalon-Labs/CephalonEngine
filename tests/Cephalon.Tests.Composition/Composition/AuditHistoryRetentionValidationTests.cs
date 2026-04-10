using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class AuditHistoryRetentionValidationTests
{
    [Fact]
    public void AddCephalonRejectsRetentionWithoutStartupOrIntervalTrigger()
    {
        var services = new ServiceCollection();
        var settings = new EngineSettings(
            blueprint: "ModularMonolith",
            databases: new DatabaseTopologySettings(
                history: new DatabaseTargetSettings(
                    provider: "Sqlite",
                    connectionString: "Data Source=history.db")),
            audit: new AuditSettings(
                enabled: true,
                history: new AuditHistorySettings(
                    enabled: true,
                    provider: "entity-framework",
                    databaseRole: "history",
                    retention: new AuditHistoryRetentionSettings(
                        enabled: true,
                        maxAgeDays: 30))));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.UseSettings(settings);
                engine.AddModule(new PlatformTestModule());
            }));

        Assert.Contains("RunIntervalMinutes", exception.Message, StringComparison.Ordinal);
        Assert.Contains("startup", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
