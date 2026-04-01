using Cephalon.Engine.Configuration;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.Technologies;
using Cephalon.Engine.Transports;

namespace Cephalon.Engine.AppModel;

public static class AppProfileFactory
{
    public static Abstractions.AppModel.AppProfile Create(EngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var builder = new AppProfileBuilder();
        ApplySettings(builder, settings);
        return builder.Build();
    }

    internal static void ApplySettings(AppProfileBuilder builder, EngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.IsNullOrWhiteSpace(settings.Blueprint))
        {
            builder.UseBlueprint(BuiltInBlueprints.Resolve(settings.Blueprint));
        }

        foreach (var patternName in settings.Patterns)
        {
            builder.AddPattern(BuiltInPatterns.Resolve(patternName));
        }

        foreach (var transportName in settings.Transports)
        {
            builder.AddTransport(BuiltInTransports.Resolve(transportName));
        }

        foreach (var technologyName in settings.Technologies)
        {
            builder.SelectTechnology(technologyName);
        }
    }
}
