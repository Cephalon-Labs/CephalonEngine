using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Behaviors.Services;

/// <summary>Reads the Engine:Behaviors configuration section and contributes topology descriptors for each entry.</summary>
internal sealed class ConfigBehaviorContributor : IBehaviorContributor
{
    private readonly IConfiguration _configuration;
    private readonly BehaviorTopologyResolver _resolver;

    /// <summary>Initializes a new instance of <see cref="ConfigBehaviorContributor"/>.</summary>
    public ConfigBehaviorContributor(IConfiguration configuration, BehaviorTopologyResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(resolver);
        _configuration = configuration;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public void RegisterBehaviors(IBehaviorRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var behaviorsSection = _configuration.GetSection(BehaviorOptions.SectionName);
        foreach (var child in behaviorsSection.GetChildren())
        {
            var behaviorId = child.Key;
            var entry = ReadEntry(child);

            var descriptor = _resolver.Resolve(behaviorId, entry);
            registry.Add(descriptor);
        }
    }

    private static BehaviorConfigEntry ReadEntry(IConfigurationSection section)
    {
        var entry = new BehaviorConfigEntry
        {
            Pattern = section[nameof(BehaviorConfigEntry.Pattern)],
            InboxEnabled = string.Equals(section[nameof(BehaviorConfigEntry.InboxEnabled)], "true", StringComparison.OrdinalIgnoreCase),
            OutboxEnabled = string.Equals(section[nameof(BehaviorConfigEntry.OutboxEnabled)], "true", StringComparison.OrdinalIgnoreCase),
            EventSourcingEnabled = string.Equals(section[nameof(BehaviorConfigEntry.EventSourcingEnabled)], "true", StringComparison.OrdinalIgnoreCase),
        };

        var transportSection = section.GetSection(nameof(BehaviorConfigEntry.Transport));
        foreach (var t in transportSection.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(t.Value))
                entry.Transport.Add(t.Value);
        }

        return entry;
    }
}
