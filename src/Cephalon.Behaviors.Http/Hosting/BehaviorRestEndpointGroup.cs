using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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
        OpenApiDocumentName = $"v{major}";
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
        => MapBehaviorCore<TBehavior>(MapGetCoreMethod, pattern, configure);

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
        => MapBehaviorCore<TBehavior>(MapPostCoreMethod, pattern, configure);

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
        => MapBehaviorCore<TBehavior>(MapPutCoreMethod, pattern, configure);

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
        => MapBehaviorCore<TBehavior>(MapPatchCoreMethod, pattern, configure);

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
        => MapBehaviorCore<TBehavior>(MapDeleteCoreMethod, pattern, configure);

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

        var documentedStatusCodes = ResolveDocumentedStatusCodes(endpoints.ServiceProvider);
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
        string pattern,
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
        var builder = group.Routes.MapGet(
            pattern,
            static (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher));
        return ApplyEndpointConventions<TInput, TOutput>(builder, contract, acceptsBody: false);
    }

    private static RouteHandlerBuilder MapBehaviorPostCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var builder = group.Routes.MapPost(
            pattern,
            static (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher));
        return ApplyEndpointConventions<TInput, TOutput>(builder, contract, acceptsBody: true);
    }

    private static RouteHandlerBuilder MapBehaviorPutCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var builder = group.Routes.MapPut(
            pattern,
            static (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher));
        return ApplyEndpointConventions<TInput, TOutput>(builder, contract, acceptsBody: true);
    }

    private static RouteHandlerBuilder MapBehaviorPatchCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var builder = group.Routes.MapMethods(
            pattern,
            ["PATCH"],
            static (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher));
        return ApplyEndpointConventions<TInput, TOutput>(builder, contract, acceptsBody: true);
    }

    private static RouteHandlerBuilder MapBehaviorDeleteCore<TBehavior, TInput, TOutput>(
        BehaviorRestEndpointGroup group,
        string pattern,
        BehaviorRestEndpointContract contract)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var builder = group.Routes.MapDelete(
            pattern,
            static (HttpContext context, BehaviorDispatcher dispatcher) =>
                InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(context, dispatcher));
        return ApplyEndpointConventions<TInput, TOutput>(builder, contract, acceptsBody: false);
    }

    private static RouteHandlerBuilder ApplyEndpointConventions<TInput, TOutput>(
        RouteHandlerBuilder builder,
        BehaviorRestEndpointContract contract,
        bool acceptsBody)
    {
        builder.WithName(contract.OperationName);
        builder.WithGroupName(contract.OpenApiDocumentName);
        builder.WithTags(contract.TagName);
        builder.WithSummary(contract.Summary);
        if (!string.IsNullOrWhiteSpace(contract.Description))
        {
            builder.WithDescription(contract.Description);
        }

        builder.WithMetadata(new BehaviorRestEndpointMetadata(
            contract.ModuleId,
            contract.ModuleVersion,
            contract.ModuleVersionMajor,
            contract.BehaviorId,
            contract.OperationName,
            contract.Summary,
            contract.Description,
            contract.TagName,
            contract.OpenApiDocumentName,
            contract.ApiVersionMajor));

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
    }

    private static async Task<IResult> InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        return await InvokeAsync<TBehavior, TInput, TOutput>(context, dispatcher, acceptsBody: false).ConfigureAwait(false);
    }

    private static async Task<IResult> InvokeWithBodyAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        return await InvokeAsync<TBehavior, TInput, TOutput>(context, dispatcher, acceptsBody: true).ConfigureAwait(false);
    }

    private static async Task<IResult> InvokeAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher,
        bool acceptsBody)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        var behaviorId = BehaviorRestEndpointContract.GetBehaviorId(typeof(TBehavior));

        try
        {
            var input = await BehaviorRequestJsonComposer.ComposeAsync<TInput>(context, acceptsBody).ConfigureAwait(false);
            var behaviorContext = DefaultBehaviorContext.From(context, behaviorId);
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

    private static HashSet<int> ResolveDocumentedStatusCodes(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = services.GetService<IConfiguration>();
        return configuration is null
            ? new HashSet<int>(new OpenApiEndpointOptions().BehaviorRestDocumentedStatusCodes)
            : new HashSet<int>(OpenApiEndpointOptions.FromConfiguration(configuration).BehaviorRestDocumentedStatusCodes);
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

    private sealed record BehaviorRestEndpointMetadata(
        string ModuleId,
        string? ModuleVersion,
        int? ModuleVersionMajor,
        string BehaviorId,
        string OperationName,
        string? Summary,
        string? Description,
        string TagName,
        string OpenApiDocumentName,
        int? ApiVersionMajor);

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
        IReadOnlySet<int> DocumentedStatusCodes)
    {
        internal static BehaviorRestEndpointContract Create(
            Type behaviorType,
            ModuleDescriptor moduleDescriptor,
            string tagName,
            int? moduleVersionMajor,
            string openApiDocumentName,
            int? apiVersionMajor,
            IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);
            ArgumentNullException.ThrowIfNull(moduleDescriptor);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
            ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentName);
            ArgumentNullException.ThrowIfNull(services);

            var contractInterface = behaviorType.GetInterfaces()
                .FirstOrDefault(static candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
                ?? throw new InvalidOperationException(
                    $"Behavior type '{behaviorType.FullName}' does not implement IAppBehavior<TInput, TOutput>.");
            var typeArguments = contractInterface.GetGenericArguments();
            var behaviorId = GetBehaviorId(behaviorType);
            var xmlSummary = BehaviorXmlDocumentation.GetSummary(behaviorType);
            var summary = xmlSummary
                ?? behaviorId;
            var description = BehaviorXmlDocumentation.GetDescription(behaviorType);
            if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(xmlSummary))
            {
                description = moduleDescriptor.Description;
            }

            var operationVersionMajor = apiVersionMajor ?? moduleVersionMajor;
            var operationName = BuildOperationName(moduleDescriptor.Id, operationVersionMajor, behaviorId);
            var outputType = typeArguments[1];
            var returnsBehaviorResult = TryResolveBehaviorResultPayloadType(outputType, out var responseType);

            var configuration = services.GetService<IConfiguration>();
            var useResultModelEnvelope = configuration is not null &&
                ApiRoutesOptions.FromConfiguration(configuration).UseResultModelEnvelope;
            var documentedStatusCodes = ResolveDocumentedStatusCodes(services);

            return new BehaviorRestEndpointContract(
                moduleDescriptor.Id,
                moduleDescriptor.Version,
                moduleVersionMajor,
                behaviorId,
                operationName,
                tagName,
                summary,
                description,
                openApiDocumentName,
                operationVersionMajor,
                typeArguments[0],
                outputType,
                responseType,
                returnsBehaviorResult,
                useResultModelEnvelope,
                documentedStatusCodes);
        }

        internal bool ShouldDocumentStatus(int statusCode) => DocumentedStatusCodes.Contains(statusCode);

        internal static string GetBehaviorId(Type behaviorType)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);

            return behaviorType.GetCustomAttribute<AppBehaviorAttribute>(inherit: false)?.Id
                ?? throw new InvalidOperationException(
                    $"Behavior type '{behaviorType.FullName}' is missing [AppBehavior].");
        }

        private static string BuildOperationName(string moduleId, int? moduleVersionMajor, string behaviorId)
        {
            var versionSegment = moduleVersionMajor.HasValue
                ? $"v{moduleVersionMajor.Value}"
                : "v0";
            return $"{NormalizeSegment(moduleId)}.{versionSegment}.{NormalizeSegment(behaviorId)}";
        }

        private static string NormalizeSegment(string value)
        {
            return string.Concat(value.Select(static ch =>
                char.IsLetterOrDigit(ch) ? ch : '_'));
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
