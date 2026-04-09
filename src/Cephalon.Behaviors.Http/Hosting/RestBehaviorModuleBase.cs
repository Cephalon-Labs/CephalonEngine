using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Base class for modules that own behaviors and expose a public REST surface for some of them.
/// </summary>
/// <remarks>
/// <para>
/// This base class keeps behavior ownership host-agnostic through <see cref="BehaviorModuleBase"/>
/// while moving the common REST authoring path into a single REST-focused DSL. Behaviors mapped
/// through <see cref="ConfigureRestBehaviors(IRestBehaviorModuleBuilder)"/> are owned by the module
/// automatically, which removes the need to declare the same public behavior in both
/// <c>ConfigureBehaviors(...)</c> and <c>MapEndpoints(...)</c>.
/// </para>
/// <para>
/// Use <see cref="BehaviorModuleBase"/> instead when a module owns behaviors but does not expose
/// a public REST surface. Use <see cref="MapAdditionalEndpoints(IEndpointRouteBuilder)"/> only for
/// advanced/custom Minimal API work that falls outside the default REST behavior DSL.
/// </para>
/// </remarks>
public abstract class RestBehaviorModuleBase : BehaviorModuleBase, IRestModule
{
    private static readonly MethodInfo DeleteMethod = GetRequiredMapMethod(nameof(BehaviorRestEndpointGroup.MapBehaviorDelete));
    private static readonly MethodInfo GetMethod = GetRequiredMapMethod(nameof(BehaviorRestEndpointGroup.MapBehaviorGet));
    private static readonly MethodInfo PatchMethod = GetRequiredMapMethod(nameof(BehaviorRestEndpointGroup.MapBehaviorPatch));
    private static readonly MethodInfo PostMethod = GetRequiredMapMethod(nameof(BehaviorRestEndpointGroup.MapBehaviorPost));
    private static readonly MethodInfo PutMethod = GetRequiredMapMethod(nameof(BehaviorRestEndpointGroup.MapBehaviorPut));

    private RestBehaviorModuleDefinition? definition;
    private bool ownershipRegistered;

    /// <summary>
    /// Declares the module-owned behaviors and their public REST surface in one place.
    /// </summary>
    /// <param name="behaviors">The REST behavior-module builder.</param>
    public abstract void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors);

    /// <summary>
    /// Adds any advanced/manual Minimal API endpoints that are not covered by the default REST behavior DSL.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder receiving additional module endpoints.</param>
    /// <remarks>
    /// When a custom endpoint still dispatches into a Cephalon behavior, declare ownership first through
    /// <see cref="ConfigureRestBehaviors(IRestBehaviorModuleBuilder)"/> using <c>Own&lt;TBehavior&gt;()</c>
    /// so engine composition remains deterministic.
    /// </remarks>
    protected virtual void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <inheritdoc />
    public sealed override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        ArgumentNullException.ThrowIfNull(behaviors);

        var moduleDefinition = GetOrCreateDefinition();
        if (ownershipRegistered)
        {
            return;
        }

        foreach (var registration in moduleDefinition.OwnershipRegistrations)
        {
            registration(behaviors);
        }

        ownershipRegistered = true;
    }

    /// <inheritdoc />
    void IRestModule.MapRestEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var moduleDefinition = GetOrCreateDefinition();
        foreach (var groupDefinition in moduleDefinition.Groups)
        {
            MapGroup(endpoints, groupDefinition);
        }

        MapAdditionalEndpoints(endpoints);
    }

    private RestBehaviorModuleDefinition GetOrCreateDefinition()
    {
        if (definition is not null)
        {
            return definition;
        }

        var builder = new RestBehaviorModuleBuilder();
        ConfigureRestBehaviors(builder);
        definition = builder.Build();
        return definition;
    }

    private void MapGroup(
        IEndpointRouteBuilder endpoints,
        RestBehaviorEndpointGroupDefinition definition)
    {
        var group = endpoints.MapBehaviorRestGroup(this, definition.Prefix);
        if (!string.IsNullOrWhiteSpace(definition.TagName))
        {
            group.WithTagName(definition.TagName);
        }

        if (definition.HasExplicitTagDescription)
        {
            group.WithTagDescription(definition.TagDescription);
        }

        if (definition.HasExplicitApiVersion && definition.ApiVersionMajor.HasValue)
        {
            group.ApiVersion(definition.ApiVersionMajor.Value);
        }

        foreach (var convention in definition.GroupConventions)
        {
            convention(group.Routes);
        }

        foreach (var endpoint in definition.Endpoints)
        {
            MapEndpoint(group, endpoint);
        }
    }

    private static void MapEndpoint(
        BehaviorRestEndpointGroup group,
        RestBehaviorEndpointDefinition endpoint)
    {
        var method = endpoint.Method switch
        {
            RestBehaviorHttpMethod.Get => GetMethod,
            RestBehaviorHttpMethod.Post => PostMethod,
            RestBehaviorHttpMethod.Put => PutMethod,
            RestBehaviorHttpMethod.Patch => PatchMethod,
            RestBehaviorHttpMethod.Delete => DeleteMethod,
            _ => throw new InvalidOperationException($"Unsupported REST behavior HTTP method '{endpoint.Method}'.")
        };

        var closedMethod = method.MakeGenericMethod(endpoint.BehaviorType);
        _ = closedMethod.Invoke(group, [endpoint.Pattern, endpoint.ConfigureEndpoint]);
    }

    private static MethodInfo GetRequiredMapMethod(string methodName)
    {
        return typeof(BehaviorRestEndpointGroup)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method =>
                string.Equals(method.Name, methodName, StringComparison.Ordinal) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters() is [{ ParameterType: var patternType }, { ParameterType: var configureType }] &&
                patternType == typeof(string) &&
                configureType == typeof(Action<RouteHandlerBuilder>));
    }
}
