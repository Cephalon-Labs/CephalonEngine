using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Hosting;

/// <summary>Contributes a single behavior topology descriptor built from a fluent configuration callback.</summary>
internal sealed class FluentBehaviorContributor<TBehavior> : IBehaviorContributor
    where TBehavior : class
{
    private readonly Action<BehaviorTopologyBuilder>? _configure;

    /// <summary>Initializes a new instance of <see cref="FluentBehaviorContributor{TBehavior}"/>.</summary>
    public FluentBehaviorContributor(Action<BehaviorTopologyBuilder>? configure)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public IReadOnlyList<BehaviorTopologyDescriptor> Contribute()
    {
        var behaviorId = GetBehaviorId();
        var builder = new BehaviorTopologyBuilder();
        _configure?.Invoke(builder);
        return [builder.Build(behaviorId)];
    }

    private static string GetBehaviorId()
    {
        // Check for [AppBehavior] attribute first
        var attr = (AppBehaviorAttribute?)Attribute.GetCustomAttribute(typeof(TBehavior), typeof(AppBehaviorAttribute));
        if (attr is not null)
            return attr.Id;

        // Fall back to type name in kebab-case
        var name = typeof(TBehavior).Name;
        return ToKebabCase(name);
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var chars = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                chars.Append('-');
            chars.Append(char.ToLowerInvariant(c));
        }
        return chars.ToString();
    }
}
