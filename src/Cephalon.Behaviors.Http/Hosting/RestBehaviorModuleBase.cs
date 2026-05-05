using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Modules;
using Cephalon.Engine.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    private RestBehaviorModuleProjection? projection;
    private bool ownershipRegistered;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        base.ConfigureServices(services);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, RestBehaviorGovernanceDiagnosticsConventionContributor>());
    }

    /// <summary>
    /// Gets the marker type used to resolve generated REST profile metadata for the current module.
    /// </summary>
    /// <returns>
    /// The marker type whose assembly Cephalon should treat as the source for generated REST
    /// profile hints when <c>MapGeneratedProfiles(...)</c> or
    /// <c>MapGeneratedProfileGroups(...)</c> is used.
    /// </returns>
    /// <remarks>
    /// Most modules should use the default implementation, which points at the concrete module
    /// type itself. Low-code wrappers can override this to point at a stable marker type from the
    /// behavior assembly when the module instance is implemented by a reusable helper type. The
    /// marker assembly must expose source-generated profile hints; generated-profile mapping does
    /// not scan assemblies for attributed behavior types.
    /// </remarks>
    protected virtual Type GetRestBehaviorProfileSourceType()
        => GetType();

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
    /// <see cref="ConfigureRestBehaviors(IRestBehaviorModuleBuilder)"/> using
    /// <c>Internal&lt;TBehavior&gt;()</c> so engine composition remains deterministic.
    /// </remarks>
    protected virtual void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <inheritdoc />
    public sealed override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        ArgumentNullException.ThrowIfNull(behaviors);

        var moduleProjection = GetOrCreateProjection();
        if (ownershipRegistered)
        {
            return;
        }

        foreach (var registration in moduleProjection.OwnershipRegistrations)
        {
            registration(behaviors);
        }

        ownershipRegistered = true;
    }

    /// <inheritdoc />
    void IRestModule.MapRestEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RestBehaviorProjectionMaterializer.MapModule(endpoints, this, GetOrCreateProjection());
        MapAdditionalEndpoints(endpoints);
    }

    private RestBehaviorModuleProjection GetOrCreateProjection()
    {
        if (projection is not null)
        {
            return projection;
        }

        var builder = new RestBehaviorModuleBuilder(GetRestBehaviorProfileSourceType());
        ConfigureRestBehaviors(builder);
        projection = builder.Build();
        return projection;
    }
}
