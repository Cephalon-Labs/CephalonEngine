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
    public IReadOnlyList<BehaviorTopologyDescriptor> Contribute()
    {
        var results = new List<BehaviorTopologyDescriptor>();

        var behaviorsSection = _configuration.GetSection("Engine:Behaviors");
        foreach (var child in behaviorsSection.GetChildren())
        {
            var behaviorId = child.Key;
            var descriptor = _resolver.Resolve(behaviorId);
            results.Add(descriptor);
        }

        return results;
    }
}
