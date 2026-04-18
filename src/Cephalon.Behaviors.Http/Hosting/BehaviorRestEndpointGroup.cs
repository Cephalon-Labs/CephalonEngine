using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Engine.Configuration;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Wraps a Minimal API route group so REST endpoints can dispatch directly into Cephalon behaviors
/// while keeping module metadata, OpenAPI tags, and version defaults aligned.
/// </summary>
public sealed class BehaviorRestEndpointGroup : IEndpointConventionBuilder
{
    private const string DefaultOpenApiDocumentName = "v1";
    private static readonly MethodInfo MapDeleteCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorDeleteCore));
    private static readonly MethodInfo MapGetCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorGetCore));
    private static readonly MethodInfo MapPatchCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorPatchCore));
    private static readonly MethodInfo MapPostCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorPostCore));
    private static readonly MethodInfo MapPutCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorPutCore));

    private readonly IEndpointRouteBuilder endpoints;
    private readonly string? moduleSummary;
    private readonly string? moduleRemarks;
    private readonly string routePrefix;
    private bool hasExplicitOpenApiDocumentName;
    private string? runtimeCandidateId;
    private string? runtimeOriginalDescription;
    private string? runtimeOriginalEndpointName;
    private RestEndpointCandidateProjectionDescriptor? runtimeOriginalProjection;
    private string? runtimeOriginalSummary;
    private string[] runtimeSkippedOverrideIds = [];
    private string[] runtimeSkippedSuppressionIds = [];
    private string[] runtimeMatchedOverrideIds = [];
    private string? runtimeSelectedOverrideId;
    private RestEndpointOverrideActionKind[] runtimeSelectedOverrideActionKinds = [];
    private RestEndpointGovernanceRuleSelectionBasis? runtimeOverrideSelectionBasis;
    private string runtimeAuthoringStyle = RestEndpointRuntimeMetadata.BehaviorHelperAuthoringStyle;
    private string runtimeSourceKind = RestEndpointRuntimeMetadata.ManualSourceKind;
    private RouteGroupBuilder? routes;

    internal BehaviorRestEndpointGroup(IEndpointRouteBuilder endpoints, IModule module, string routePrefix)
    {
        this.endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        this.routePrefix = NormalizeRoutePrefix(routePrefix);
        ModuleDescriptor = module.Descriptor;
        ModuleVersionMajor = ResolveModuleMajorVersion(ModuleDescriptor.Version);
        moduleSummary = BehaviorXmlDocumentation.GetSummary(Module.GetType());
        moduleRemarks = BehaviorXmlDocumentation.GetRemarks(Module.GetType());
        TagName = ModuleDescriptor.DisplayName;
        TagDescription = BuildTagDescription(
            moduleSummary,
            moduleRemarks,
            ModuleDescriptor.Description);
        OpenApiDocumentName = ModuleVersionMajor.HasValue
            ? $"v{ModuleVersionMajor.Value}"
            : DefaultOpenApiDocumentName;
    }

    /// <summary>
    /// Gets the underlying Minimal API route group.
    /// </summary>
    public RouteGroupBuilder Routes => EnsureRoutes();

    /// <summary>
    /// Gets the module instance that owns the route group.
    /// </summary>
    public IModule Module { get; }

    /// <summary>
    /// Gets the descriptor for the owning module.
    /// </summary>
    public ModuleDescriptor ModuleDescriptor { get; }

    /// <summary>
    /// Gets the OpenAPI tag name used for endpoints in this group.
    /// </summary>
    public string TagName { get; private set; }

    /// <summary>
    /// Gets the OpenAPI tag description used for endpoints in this group when one is available.
    /// </summary>
    public string? TagDescription { get; private set; }

    /// <summary>
    /// Gets the major version parsed from the module descriptor when available.
    /// </summary>
    public int? ModuleVersionMajor { get; }

    /// <summary>
    /// Gets the OpenAPI document name that newly mapped endpoints join by default.
    /// </summary>
    public string OpenApiDocumentName { get; private set; }

    internal string ResolvedRoutePrefix => BuildResolvedRoutePrefix();

    internal void UseRuntimeSourceKind(string sourceKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKind);
        runtimeSourceKind = sourceKind.Trim();
    }

    internal void UseRuntimeAuthoringStyle(string authoringStyle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);
        runtimeAuthoringStyle = authoringStyle.Trim();
    }

    internal void UseRuntimeCandidateId(string? candidateId)
    {
        runtimeCandidateId = string.IsNullOrWhiteSpace(candidateId)
            ? null
            : candidateId.Trim();
    }

    internal void UseRuntimeOriginalProjection(RestEndpointCandidateProjectionDescriptor? originalProjection)
    {
        runtimeOriginalProjection = originalProjection is null
            ? null
            : new RestEndpointCandidateProjectionDescriptor(
                originalProjection.Method,
                originalProjection.RoutePattern,
                originalProjection.RouteGroupPrefix,
                originalProjection.RelativePattern,
                originalProjection.ApiVersionMajor,
                originalProjection.OpenApiDocumentName,
                originalProjection.BindingDescriptors,
                originalProjection.BindingFallbackMode,
                originalProjection.TagName,
                originalProjection.AllowsHostGovernance,
                originalProjection.HostGovernanceScope);
    }

    internal void UseRuntimeOriginalEndpointMetadata(
        string? endpointName,
        string? summary,
        string? description)
    {
        runtimeOriginalEndpointName = NormalizeOptionalRuntimeMetadataValue(endpointName);
        runtimeOriginalSummary = NormalizeOptionalRuntimeMetadataValue(summary);
        runtimeOriginalDescription = NormalizeOptionalRuntimeMetadataValue(description);
    }

    internal void UseRuntimeMatchedOverrideIds(IReadOnlyList<string>? matchedOverrideIds)
    {
        runtimeMatchedOverrideIds = matchedOverrideIds?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    internal void UseRuntimeSelectedOverride(
        string? selectedOverrideId,
        IReadOnlyList<RestEndpointOverrideActionKind>? selectedOverrideActionKinds)
    {
        runtimeSelectedOverrideId = string.IsNullOrWhiteSpace(selectedOverrideId)
            ? null
            : selectedOverrideId.Trim();
        runtimeSelectedOverrideActionKinds = NormalizeRuntimeActionKinds(selectedOverrideActionKinds);
    }

    internal void UseRuntimeSkippedGovernanceRuleIds(
        IReadOnlyList<string>? skippedSuppressionIds,
        IReadOnlyList<string>? skippedOverrideIds)
    {
        runtimeSkippedSuppressionIds = skippedSuppressionIds?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        runtimeSkippedOverrideIds = skippedOverrideIds?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    internal void UseRuntimeOverrideSelectionBasis(
        RestEndpointGovernanceRuleSelectionBasis? overrideSelectionBasis)
    {
        if (overrideSelectionBasis.HasValue &&
            (!Enum.IsDefined(overrideSelectionBasis.Value) ||
             overrideSelectionBasis.Value == RestEndpointGovernanceRuleSelectionBasis.Unspecified))
        {
            throw new ArgumentException(
                "A supported REST endpoint governance rule selection basis is required when one is declared.",
                nameof(overrideSelectionBasis));
        }

        runtimeOverrideSelectionBasis = overrideSelectionBasis;
    }

    /// <summary>
    /// Gets the explicit API major version applied to newly mapped endpoints when configured.
    /// </summary>
    public int? ApiVersionMajor { get; private set; }

    /// <summary>
    /// Assigns subsequently mapped endpoints to the OpenAPI document represented by the supplied API major version.
    /// </summary>
    /// <param name="major">The API major version to apply.</param>
    /// <returns>The same group instance for fluent endpoint composition.</returns>
    /// <remarks>
    /// Call this before mapping endpoints when a module needs its REST surface to appear in a document other than the default <c>v1</c>.
    /// </remarks>
    public BehaviorRestEndpointGroup ApiVersion(int major)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(major);
        EnsureRoutesNotCreated(nameof(ApiVersion));

        ApiVersionMajor = major;
        if (!hasExplicitOpenApiDocumentName)
        {
            OpenApiDocumentName = $"v{major}";
        }

        return this;
    }

    /// <summary>
    /// Overrides the OpenAPI document name applied to subsequently mapped endpoints in this group.
    /// </summary>
    /// <param name="openApiDocumentName">The OpenAPI document name to publish.</param>
    /// <returns>The same group instance for fluent endpoint composition.</returns>
    public BehaviorRestEndpointGroup WithOpenApiDocumentName(string openApiDocumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentName);
        EnsureRoutesNotCreated(nameof(WithOpenApiDocumentName));

        OpenApiDocumentName = openApiDocumentName.Trim();
        hasExplicitOpenApiDocumentName = true;
        return this;
    }

    /// <summary>
    /// Overrides the OpenAPI tag name applied to subsequently mapped endpoints in this group.
    /// </summary>
    /// <param name="tagName">The public tag name to publish.</param>
    /// <returns>The same group instance for fluent endpoint composition.</returns>
    public BehaviorRestEndpointGroup WithTagName(string tagName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        EnsureRoutesNotCreated(nameof(WithTagName));

        TagName = tagName.Trim();
        return this;
    }

    /// <summary>
    /// Overrides the OpenAPI tag description applied to this group.
    /// </summary>
    /// <param name="description">The tag description to publish. Pass <see langword="null" /> to clear it.</param>
    /// <returns>The same group instance for fluent endpoint composition.</returns>
    public BehaviorRestEndpointGroup WithTagDescription(string? description)
    {
        EnsureRoutesNotCreated(nameof(WithTagDescription));

        TagDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        return this;
    }

    /// <summary>
    /// Maps a REST <c>GET</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to dispatch.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configure">
    /// Optional callback for additional Minimal API conventions such as authorization or custom
    /// response metadata.
    /// </param>
    /// <returns>The route handler builder for further customization.</returns>
    public RouteHandlerBuilder MapBehaviorGet<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorGet<TBehavior>(pattern, [], preserveImplicitQueryFallback: false, configure);

    internal RouteHandlerBuilder MapBehaviorGet<TBehavior>(
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorCore<TBehavior>(MapGetCoreMethod, RestBehaviorHttpMethod.Get, pattern, bindings, preserveImplicitQueryFallback, configure);

    /// <summary>
    /// Maps a REST <c>POST</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to dispatch.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configure">
    /// Optional callback for additional Minimal API conventions such as authorization or custom
    /// response metadata.
    /// </param>
    /// <returns>The route handler builder for further customization.</returns>
    public RouteHandlerBuilder MapBehaviorPost<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorPost<TBehavior>(pattern, [], preserveImplicitQueryFallback: false, configure);

    internal RouteHandlerBuilder MapBehaviorPost<TBehavior>(
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorCore<TBehavior>(MapPostCoreMethod, RestBehaviorHttpMethod.Post, pattern, bindings, preserveImplicitQueryFallback, configure);

    /// <summary>
    /// Maps a REST <c>PUT</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to dispatch.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configure">
    /// Optional callback for additional Minimal API conventions such as authorization or custom
    /// response metadata.
    /// </param>
    /// <returns>The route handler builder for further customization.</returns>
    public RouteHandlerBuilder MapBehaviorPut<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorPut<TBehavior>(pattern, [], preserveImplicitQueryFallback: false, configure);

    internal RouteHandlerBuilder MapBehaviorPut<TBehavior>(
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorCore<TBehavior>(MapPutCoreMethod, RestBehaviorHttpMethod.Put, pattern, bindings, preserveImplicitQueryFallback, configure);

    /// <summary>
    /// Maps a REST <c>PATCH</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to dispatch.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configure">
    /// Optional callback for additional Minimal API conventions such as authorization or custom
    /// response metadata.
    /// </param>
    /// <returns>The route handler builder for further customization.</returns>
    public RouteHandlerBuilder MapBehaviorPatch<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorPatch<TBehavior>(pattern, [], preserveImplicitQueryFallback: false, configure);

    internal RouteHandlerBuilder MapBehaviorPatch<TBehavior>(
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorCore<TBehavior>(MapPatchCoreMethod, RestBehaviorHttpMethod.Patch, pattern, bindings, preserveImplicitQueryFallback, configure);

    /// <summary>
    /// Maps a REST <c>DELETE</c> endpoint that dispatches into the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to dispatch.</typeparam>
    /// <param name="pattern">The route pattern relative to the group prefix.</param>
    /// <param name="configure">
    /// Optional callback for additional Minimal API conventions such as authorization or custom
    /// response metadata.
    /// </param>
    /// <returns>The route handler builder for further customization.</returns>
    public RouteHandlerBuilder MapBehaviorDelete<TBehavior>(
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorDelete<TBehavior>(pattern, [], preserveImplicitQueryFallback: false, configure);

    internal RouteHandlerBuilder MapBehaviorDelete<TBehavior>(
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback,
        Action<RouteHandlerBuilder>? configure = null)
        where TBehavior : class
        => MapBehaviorCore<TBehavior>(MapDeleteCoreMethod, RestBehaviorHttpMethod.Delete, pattern, bindings, preserveImplicitQueryFallback, configure);

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        ((IEndpointConventionBuilder)EnsureRoutes()).Add(convention);
    }

    private RouteGroupBuilder EnsureRoutes()
    {
        if (routes is not null)
        {
            return routes;
        }

        var documentedStatusCodes = ResolveDocumentedStatusCodes(endpoints.ServiceProvider, "rest-api");
        routes = endpoints.MapGroup(BuildResolvedRoutePrefix());
        routes.WithGroupName(OpenApiDocumentName);
        routes.WithTags(TagName);
        if (documentedStatusCodes.Contains(StatusCodes.Status400BadRequest))
        {
            routes.ProducesProblem(StatusCodes.Status400BadRequest);
        }

        if (documentedStatusCodes.Contains(StatusCodes.Status404NotFound))
        {
            routes.ProducesProblem(StatusCodes.Status404NotFound);
        }
        routes.WithMetadata(new OpenApiTagMetadata(TagName, TagDescription));
        routes.WithMetadata(new BehaviorRestGroupMetadata(
            ModuleDescriptor.Id,
            ModuleDescriptor.DisplayName,
            ModuleDescriptor.Description,
            ModuleDescriptor.Version,
            ModuleVersionMajor,
            moduleSummary,
            moduleRemarks,
            TagName,
            TagDescription));

        return routes;
    }

    private string BuildResolvedRoutePrefix()
    {
        var resolvedPrefix = routePrefix;
        var resolvedApiVersionMajor = ApiVersionMajor ?? ModuleVersionMajor;
        if (!resolvedApiVersionMajor.HasValue)
        {
            return resolvedPrefix;
        }

        var versionPrefix = $"/v{resolvedApiVersionMajor.Value}";
        if (resolvedPrefix.Equals(versionPrefix, StringComparison.OrdinalIgnoreCase) ||
            resolvedPrefix.StartsWith($"{versionPrefix}/", StringComparison.OrdinalIgnoreCase))
        {
            return resolvedPrefix;
        }

        return $"{versionPrefix}{resolvedPrefix}";
    }

    private RouteHandlerBuilder MapBehaviorCore<TBehavior>(
        MethodInfo coreMethod,
        RestBehaviorHttpMethod method,
        string pattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback,
        Action<RouteHandlerBuilder>? configure)
        where TBehavior : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ValidateBehaviorOwnership(typeof(TBehavior));

        var contract = BehaviorRestEndpointContract.Create(
            typeof(TBehavior),
            ModuleDescriptor,
            TagName,
            ModuleVersionMajor,
            OpenApiDocumentName,
            ApiVersionMajor,
            method,
            pattern,
            bindings,
            preserveImplicitQueryFallback,
            endpoints.ServiceProvider);
        var closedMethod = coreMethod.MakeGenericMethod(typeof(TBehavior), contract.InputType, contract.OutputType);
        var builder = (RouteHandlerBuilder)closedMethod.Invoke(null, [this, pattern, contract])!;
        configure?.Invoke(builder);
        return builder;
    }

    private void ValidateBehaviorOwnership(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var ownedBehaviors = endpoints.ServiceProvider.GetService<IReadOnlyList<OwnedBehaviorRegistration>>();
        if (ownedBehaviors is null || ownedBehaviors.Count == 0)
        {
            return;
        }

        var behaviorId = BehaviorRestEndpointContract.GetBehaviorId(behaviorType);
        var owner = ownedBehaviors.FirstOrDefault(registration =>
            string.Equals(registration.BehaviorId, behaviorId, StringComparison.OrdinalIgnoreCase));
        if (owner is null ||
            string.Equals(owner.SourceModuleId, ModuleDescriptor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Behavior '{behaviorId}' is owned by module '{owner.SourceModuleId}' and cannot be mapped by module '{ModuleDescriptor.Id}'.");
    }

    private static RouteHandlerBuilder MapBehaviorGetCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var bindings = contract.Bindings;
        var preserveImplicitQueryFallback = contract.PreserveImplicitQueryFallback;
        var builder = group.Routes.MapGet(
            pattern,
            (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher, bindings, preserveImplicitQueryFallback));
        return ApplyEndpointConventions<TBehavior, TInput, TOutput>(
                builder,
                group,
                contract,
                RestBehaviorHttpMethod.Get,
                pattern,
                acceptsBody: false)
            .ApplyCephalonRateLimiting(group.endpoints.ServiceProvider, "rest-api", contract.BehaviorId);
    }

    private static RouteHandlerBuilder MapBehaviorPostCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var bindings = contract.Bindings;
        var preserveImplicitQueryFallback = contract.PreserveImplicitQueryFallback;
        var builder = group.Routes.MapPost(
            pattern,
            (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher, bindings, preserveImplicitQueryFallback));
        return ApplyEndpointConventions<TBehavior, TInput, TOutput>(
                builder,
                group,
                contract,
                RestBehaviorHttpMethod.Post,
                pattern,
                acceptsBody: true)
            .ApplyCephalonRateLimiting(group.endpoints.ServiceProvider, "rest-api", contract.BehaviorId);
    }

    private static RouteHandlerBuilder MapBehaviorPutCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var bindings = contract.Bindings;
        var preserveImplicitQueryFallback = contract.PreserveImplicitQueryFallback;
        var builder = group.Routes.MapPut(
            pattern,
            (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher, bindings, preserveImplicitQueryFallback));
        return ApplyEndpointConventions<TBehavior, TInput, TOutput>(
                builder,
                group,
                contract,
                RestBehaviorHttpMethod.Put,
                pattern,
                acceptsBody: true)
            .ApplyCephalonRateLimiting(group.endpoints.ServiceProvider, "rest-api", contract.BehaviorId);
    }

    private static RouteHandlerBuilder MapBehaviorPatchCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var bindings = contract.Bindings;
        var preserveImplicitQueryFallback = contract.PreserveImplicitQueryFallback;
        var builder = group.Routes.MapMethods(
            pattern,
            ["PATCH"],
            (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher, bindings, preserveImplicitQueryFallback));
        return ApplyEndpointConventions<TBehavior, TInput, TOutput>(
                builder,
                group,
                contract,
                RestBehaviorHttpMethod.Patch,
                pattern,
                acceptsBody: true)
            .ApplyCephalonRateLimiting(group.endpoints.ServiceProvider, "rest-api", contract.BehaviorId);
    }

    private static RouteHandlerBuilder MapBehaviorDeleteCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var bindings = contract.Bindings;
        var preserveImplicitQueryFallback = contract.PreserveImplicitQueryFallback;
        var builder = group.Routes.MapDelete(
            pattern,
            (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher, bindings, preserveImplicitQueryFallback));
        return ApplyEndpointConventions<TBehavior, TInput, TOutput>(
                builder,
                group,
                contract,
                RestBehaviorHttpMethod.Delete,
                pattern,
                acceptsBody: false)
            .ApplyCephalonRateLimiting(group.endpoints.ServiceProvider, "rest-api", contract.BehaviorId);
    }

    private static RouteHandlerBuilder ApplyEndpointConventions<TBehavior, TInput, TOutput>(
        RouteHandlerBuilder builder,
        BehaviorRestEndpointGroup group,
        BehaviorRestEndpointContract contract,
        RestBehaviorHttpMethod method,
        string pattern,
        bool acceptsBody)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var normalizedPattern = pattern.Trim();

        builder.WithName(contract.OperationName);
        builder.WithGroupName(contract.OpenApiDocumentName);
        builder.WithTags(contract.TagName);
        builder.WithSummary(contract.Summary);
        if (!string.IsNullOrWhiteSpace(contract.Description))
        {
            builder.WithDescription(contract.Description);
        }

        builder.WithMetadata(new RestBehaviorEndpointMetadata(
            group.runtimeSourceKind,
            group.runtimeAuthoringStyle,
            contract.BehaviorId,
            typeof(TBehavior).FullName ?? typeof(TBehavior).Name,
            contract.OperationName,
            contract.Summary,
            contract.Description,
            group.runtimeOriginalEndpointName,
            group.runtimeOriginalSummary,
            group.runtimeOriginalDescription,
            contract.TagName,
            contract.OpenApiDocumentName,
            contract.ApiVersionMajor,
            group.ResolvedRoutePrefix,
            normalizedPattern,
            group.runtimeCandidateId,
            group.runtimeOriginalProjection,
            contract.Bindings.Count == 0
                ? null
                : RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(contract.Bindings),
            contract.BindingFallbackMode,
            group.runtimeSelectedOverrideId,
            group.runtimeMatchedOverrideIds,
            group.runtimeSelectedOverrideActionKinds,
            group.runtimeOverrideSelectionBasis,
            group.runtimeSkippedSuppressionIds,
            group.runtimeSkippedOverrideIds));

        ApplyResponseConventions(builder, contract);

        if (acceptsBody)
        {
            builder.Accepts(typeof(TInput), "application/json");
        }

        return builder;
    }

    private static void ApplyResponseConventions(
        RouteHandlerBuilder builder,
        BehaviorRestEndpointContract contract)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var successResponseType = contract.UseResultModelEnvelope
            ? typeof(ResultModel<>).MakeGenericType(contract.ResponseType)
            : contract.ResponseType;
        var errorResponseType = contract.UseResultModelEnvelope
            ? typeof(ResultModelError)
            : typeof(ProblemDetails);

        if (contract.ShouldDocumentStatus(StatusCodes.Status200OK))
        {
            builder.Produces(StatusCodes.Status200OK, successResponseType, "application/json");
        }

        if (contract.ReturnsBehaviorResult)
        {
            if (contract.ShouldDocumentStatus(StatusCodes.Status201Created))
            {
                builder.Produces(StatusCodes.Status201Created, successResponseType, "application/json");
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status202Accepted))
            {
                builder.Produces(StatusCodes.Status202Accepted, successResponseType, "application/json");
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status204NoContent))
            {
                builder.Produces(StatusCodes.Status204NoContent);
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status401Unauthorized))
            {
                builder.Produces(StatusCodes.Status401Unauthorized, errorResponseType, "application/json");
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status403Forbidden))
            {
                builder.Produces(StatusCodes.Status403Forbidden, errorResponseType, "application/json");
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status409Conflict))
            {
                builder.Produces(StatusCodes.Status409Conflict, errorResponseType, "application/json");
            }
        }

        if (contract.UseResultModelEnvelope)
        {
            if (contract.ShouldDocumentStatus(StatusCodes.Status400BadRequest))
            {
                builder.Produces(StatusCodes.Status400BadRequest, errorResponseType, "application/json");
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status404NotFound))
            {
                builder.Produces(StatusCodes.Status404NotFound, errorResponseType, "application/json");
            }
        }
        else
        {
            if (contract.ShouldDocumentStatus(StatusCodes.Status400BadRequest))
            {
                builder.ProducesProblem(StatusCodes.Status400BadRequest);
            }

            if (contract.ShouldDocumentStatus(StatusCodes.Status404NotFound))
            {
                builder.Produces(StatusCodes.Status404NotFound);
            }
        }

        if (contract.ShouldDocumentStatus(StatusCodes.Status500InternalServerError))
        {
            if (contract.UseResultModelEnvelope)
            {
                builder.Produces(StatusCodes.Status500InternalServerError, errorResponseType, "application/json");
            }
            else
            {
                builder.ProducesProblem(StatusCodes.Status500InternalServerError);
            }
        }

        if (contract.ShouldDocumentStatus(StatusCodes.Status503ServiceUnavailable))
        {
            if (contract.UseResultModelEnvelope)
            {
                builder.Produces(StatusCodes.Status503ServiceUnavailable, errorResponseType, "application/json");
            }
            else
            {
                builder.ProducesProblem(StatusCodes.Status503ServiceUnavailable);
            }
        }

        if (contract.ShouldDocumentStatus(StatusCodes.Status429TooManyRequests))
        {
            if (contract.UseResultModelEnvelope)
            {
                builder.Produces(StatusCodes.Status429TooManyRequests, errorResponseType, "application/json");
            }
            else
            {
                builder.ProducesProblem(StatusCodes.Status429TooManyRequests);
            }
        }
    }

    private static async Task<IResult> InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        return await InvokeAsync<TBehavior, TInput, TOutput>(
            context,
            dispatcher,
            acceptsBody: false,
            bindings,
            preserveImplicitQueryFallback).ConfigureAwait(false);
    }

    private static async Task<IResult> InvokeWithBodyAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        return await InvokeAsync<TBehavior, TInput, TOutput>(
            context,
            dispatcher,
            acceptsBody: true,
            bindings,
            preserveImplicitQueryFallback).ConfigureAwait(false);
    }

    private static async Task<IResult> InvokeAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher,
        bool acceptsBody,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var behaviorId = BehaviorRestEndpointContract.GetBehaviorId(typeof(TBehavior));

        try
        {
            var input = await BehaviorRequestJsonComposer.ComposeAsync<TInput>(
                    context,
                    acceptsBody,
                    bindings,
                    preserveImplicitQueryFallback)
                .ConfigureAwait(false);
            var behaviorContext = DefaultBehaviorContext.From(context, behaviorId, "rest-api");
            var result = await dispatcher.DispatchAsync(
                behaviorId,
                input,
                behaviorContext,
                context.RequestAborted).ConfigureAwait(false);

            if (result is null)
            {
                return TypedResults.NoContent();
            }

            if (result is IBehaviorResult behaviorResult)
            {
                return BehaviorRestResponseMapper.MapBehaviorResult(behaviorResult, context.RequestServices);
            }

            return BehaviorRestResponseMapper.MapSuccess((TOutput)result, context.RequestServices);
        }
        catch (BehaviorNotFoundException)
        {
            return BehaviorRestResponseMapper.MapNotFound(
                $"Behavior '{behaviorId}' was not found.",
                context.RequestServices,
                code: behaviorId);
        }
        catch (KeyNotFoundException)
        {
            return BehaviorRestResponseMapper.MapNotFound(
                "The requested resource was not found.",
                context.RequestServices);
        }
        catch (JsonException ex)
        {
            return BehaviorRestResponseMapper.MapBadRequest(ex.Message, context.RequestServices, code: "invalid_json");
        }
        catch (TimeoutRejectedException)
        {
            return BehaviorRestResponseMapper.MapServiceUnavailable(
                "The request exceeded the configured Cephalon behavior execution timeout.",
                context.RequestServices,
                code: "behavior_execution_timeout");
        }
        catch (BrokenCircuitException)
        {
            return BehaviorRestResponseMapper.MapServiceUnavailable(
                "The request was rejected because the configured Cephalon behavior circuit breaker is open.",
                context.RequestServices,
                code: "behavior_execution_circuit_breaker_open",
                retryAfterSeconds: ResolveCircuitRetryAfterSeconds(
                    context.RequestServices,
                    behaviorId,
                    "rest-api"));
        }
        catch (RateLimiterRejectedException)
        {
            return BehaviorRestResponseMapper.MapTooManyRequests(
                "The request exceeded the configured Cephalon behavior concurrency limit.",
                context.RequestServices,
                code: "behavior_execution_rejected");
        }
        catch (InvalidOperationException ex)
        {
            return BehaviorRestResponseMapper.MapBadRequest(ex.Message, context.RequestServices, code: "invalid_operation");
        }
        catch (ArgumentException ex)
        {
            return BehaviorRestResponseMapper.MapBadRequest(ex.Message, context.RequestServices, code: "invalid_argument");
        }
    }

    private static int? ResolveModuleMajorVersion(string? moduleVersion)
    {
        return Version.TryParse(moduleVersion, out var parsedVersion)
            ? parsedVersion.Major
            : null;
    }

    private static string NormalizeRoutePrefix(string routePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);

        var normalized = routePrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;
    }

    private static string? NormalizeOptionalRuntimeMetadataValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static RestEndpointOverrideActionKind[] NormalizeRuntimeActionKinds(
        IReadOnlyList<RestEndpointOverrideActionKind>? actionKinds)
    {
        if (actionKinds is null || actionKinds.Count == 0)
        {
            return [];
        }

        var normalized = actionKinds
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value =>
                !Enum.IsDefined(value) ||
                value == RestEndpointOverrideActionKind.Unspecified))
        {
            throw new ArgumentException(
                "A supported REST endpoint override action kind is required when one is declared.",
                nameof(actionKinds));
        }

        return normalized;
    }

    private void EnsureRoutesNotCreated(string methodName)
    {
        if (routes is not null)
        {
            throw new InvalidOperationException(
                $"Call {methodName} before mapping endpoints on the behavior REST group.");
        }
    }

    private static string? BuildTagDescription(
        string? summary,
        string? remarks,
        string? fallbackDescription)
    {
        var sections = new List<string>();

        if (!string.IsNullOrWhiteSpace(summary))
        {
            sections.Add(summary.Trim());
        }

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            sections.Add(remarks.Trim());
        }

        if (sections.Count > 0)
        {
            return string.Join(Environment.NewLine + Environment.NewLine, sections);
        }

        return string.IsNullOrWhiteSpace(fallbackDescription)
            ? null
            : fallbackDescription.Trim();
    }

    private static HashSet<int> ResolveDocumentedStatusCodes(
        IServiceProvider services,
        string transportId,
        string? behaviorId = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var configuration = services.GetService<IConfiguration>();
        var statusCodes = configuration is null
            ? new HashSet<int>(new OpenApiEndpointOptions().BehaviorRestDocumentedStatusCodes)
            : new HashSet<int>(OpenApiEndpointOptions.FromConfiguration(configuration).BehaviorRestDocumentedStatusCodes);
        var behaviorResilienceCatalog = services.GetService<IBehaviorResilienceRuntimeCatalog>();
        var resolvedBehaviorResiliencePolicy = string.IsNullOrWhiteSpace(behaviorId)
            ? null
            : behaviorResilienceCatalog?.Resolve(behaviorId, transportId);
        if (resolvedBehaviorResiliencePolicy?.Effective.Timeout.Enabled == true &&
            resolvedBehaviorResiliencePolicy.Effective.Timeout.HasValues)
        {
            statusCodes.Add(StatusCodes.Status503ServiceUnavailable);
        }
        else if (resolvedBehaviorResiliencePolicy?.Effective.CircuitBreaker.Enabled == true &&
            resolvedBehaviorResiliencePolicy.Effective.CircuitBreaker.HasValues)
        {
            statusCodes.Add(StatusCodes.Status503ServiceUnavailable);
        }
        else if (resolvedBehaviorResiliencePolicy is null &&
            behaviorResilienceCatalog?.Policies.Any(static policy =>
                ((policy.Effective.Timeout.Enabled == true &&
                  policy.Effective.Timeout.HasValues) ||
                 (policy.Effective.CircuitBreaker.Enabled == true &&
                  policy.Effective.CircuitBreaker.HasValues))) == true)
        {
            statusCodes.Add(StatusCodes.Status503ServiceUnavailable);
        }

        if (resolvedBehaviorResiliencePolicy?.Effective.Bulkhead.Enabled == true &&
            resolvedBehaviorResiliencePolicy.Effective.Bulkhead.HasValues)
        {
            statusCodes.Add(StatusCodes.Status429TooManyRequests);
        }
        else if (resolvedBehaviorResiliencePolicy is null &&
            behaviorResilienceCatalog?.Policies.Any(static policy =>
                policy.Effective.Bulkhead.Enabled == true &&
                policy.Effective.Bulkhead.HasValues) == true)
        {
            statusCodes.Add(StatusCodes.Status429TooManyRequests);
        }

        if (services.HasCephalonRateLimiting(transportId, behaviorId))
        {
            statusCodes.Add(StatusCodes.Status429TooManyRequests);
        }

        return statusCodes;
    }

    private static int? ResolveCircuitRetryAfterSeconds(
        IServiceProvider services,
        string behaviorId,
        string transportId)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var behaviorResilienceCatalog = services.GetService<IBehaviorResilienceRuntimeCatalog>();
        var policy = behaviorResilienceCatalog?.Resolve(behaviorId, transportId);
        if (policy?.Metadata.TryGetValue("circuitRetryAfterSeconds", out var rawRetryAfterSeconds) != true)
        {
            return null;
        }

        return int.TryParse(rawRetryAfterSeconds, out var retryAfterSeconds)
            ? retryAfterSeconds
            : null;
    }

    private static MethodInfo GetRequiredCoreMethod(string methodName)
    {
        return typeof(BehaviorRestEndpointGroup).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Required helper method '{methodName}' was not found.");
    }

    private sealed record BehaviorRestGroupMetadata(
        string ModuleId,
        string DisplayName,
        string Description,
        string? Version,
        int? MajorVersion,
        string? Summary,
        string? Remarks,
        string TagName,
        string? TagDescription);

    private sealed record BehaviorRestEndpointContract(
        string ModuleId,
        string? ModuleVersion,
        int? ModuleVersionMajor,
        string BehaviorId,
        string OperationName,
        string TagName,
        string Summary,
        string? Description,
        string OpenApiDocumentName,
        int? ApiVersionMajor,
        Type InputType,
        Type OutputType,
        Type ResponseType,
        bool ReturnsBehaviorResult,
        bool UseResultModelEnvelope,
        IReadOnlySet<int> DocumentedStatusCodes,
        IReadOnlyList<BehaviorRestBindingDescriptor> Bindings,
        RestEndpointBindingFallbackMode? BindingFallbackMode,
        bool PreserveImplicitQueryFallback)
    {
        internal static BehaviorRestEndpointContract Create(
            Type behaviorType,
            ModuleDescriptor moduleDescriptor,
            string tagName,
            int? moduleVersionMajor,
            string openApiDocumentName,
            int? apiVersionMajor,
            RestBehaviorHttpMethod method,
            string pattern,
            IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
            bool preserveImplicitQueryFallback,
            IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);
            ArgumentNullException.ThrowIfNull(moduleDescriptor);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
            ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentName);
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
            ArgumentNullException.ThrowIfNull(bindings);
            ArgumentNullException.ThrowIfNull(services);

            var contractInterface = behaviorType.GetInterfaces()
                .FirstOrDefault(static candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
                ?? throw new InvalidOperationException(
                    $"Behavior type '{behaviorType.FullName}' does not implement IAppBehavior<TInput, TOutput>.");
            var typeArguments = contractInterface.GetGenericArguments();
            var behaviorId = GetBehaviorId(behaviorType);
            var operationVersionMajor = apiVersionMajor ?? moduleVersionMajor;
            var operationName = RestBehaviorEndpointMetadataConventions.BuildOperationName(
                moduleDescriptor.Id,
                operationVersionMajor,
                behaviorId);
            var documentation = RestBehaviorEndpointMetadataConventions.ResolveOperationDocumentation(
                behaviorType,
                moduleDescriptor,
                behaviorId);
            var outputType = typeArguments[1];
            var returnsBehaviorResult = TryResolveBehaviorResultPayloadType(outputType, out var responseType);
            var normalizedBindings = BehaviorRestBindingPlanNormalizer.NormalizeForInputType(
                $"Behavior '{behaviorId}'",
                typeArguments[0],
                method,
                pattern,
                bindings);

            var configuration = services.GetService<IConfiguration>();
            var useResultModelEnvelope = configuration is not null &&
                ApiRoutesOptions.FromConfiguration(configuration).UseResultModelEnvelope;
            var documentedStatusCodes = ResolveDocumentedStatusCodes(services, "rest-api", behaviorId);
            var bindingFallbackMode = RestBehaviorBindingFallbackModeResolver.ResolveForInputType(
                typeArguments[0],
                method,
                pattern,
                normalizedBindings,
                preserveImplicitQueryFallback);

            return new BehaviorRestEndpointContract(
                moduleDescriptor.Id,
                moduleDescriptor.Version,
                moduleVersionMajor,
                behaviorId,
                operationName,
                tagName,
                documentation.Summary,
                documentation.Description,
                openApiDocumentName,
                operationVersionMajor,
                typeArguments[0],
                outputType,
                responseType,
                returnsBehaviorResult,
                useResultModelEnvelope,
                documentedStatusCodes,
                normalizedBindings,
                bindingFallbackMode,
                preserveImplicitQueryFallback);
        }

        internal bool ShouldDocumentStatus(int statusCode) => DocumentedStatusCodes.Contains(statusCode);

        internal static string GetBehaviorId(Type behaviorType)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);

            return behaviorType.GetCustomAttribute<AppBehaviorAttribute>(inherit: false)?.Id
                ?? throw new InvalidOperationException(
                    $"Behavior type '{behaviorType.FullName}' is missing [AppBehavior].");
        }

        private static bool TryResolveBehaviorResultPayloadType(Type outputType, out Type responseType)
        {
            ArgumentNullException.ThrowIfNull(outputType);

            responseType = outputType;
            if (!outputType.IsGenericType)
            {
                return false;
            }

            var genericDefinition = outputType.GetGenericTypeDefinition();
            if (genericDefinition != typeof(Result<>) &&
                genericDefinition != typeof(BehaviorResult<>))
            {
                return false;
            }

            responseType = outputType.GetGenericArguments()[0];
            return true;
        }

    }
}
