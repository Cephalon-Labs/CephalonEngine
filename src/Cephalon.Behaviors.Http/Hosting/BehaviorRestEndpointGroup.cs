using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Wraps a Minimal API route group so REST endpoints can dispatch directly into Cephalon behaviors
/// while keeping module metadata, OpenAPI tags, and version defaults aligned.
/// </summary>
public sealed class BehaviorRestEndpointGroup : IEndpointConventionBuilder
{
    private static readonly MethodInfo MapDeleteCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorDeleteCore));
    private static readonly MethodInfo MapGetCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorGetCore));
    private static readonly MethodInfo MapPatchCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorPatchCore));
    private static readonly MethodInfo MapPostCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorPostCore));
    private static readonly MethodInfo MapPutCoreMethod = GetRequiredCoreMethod(nameof(MapBehaviorPutCore));

    internal BehaviorRestEndpointGroup(RouteGroupBuilder routes, IModule module)
    {
        Routes = routes ?? throw new ArgumentNullException(nameof(routes));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        ModuleDescriptor = module.Descriptor;
        ModuleVersionMajor = ResolveModuleMajorVersion(ModuleDescriptor.Version);
        TagName = ModuleDescriptor.DisplayName;

        Routes.WithTags(TagName);
        Routes.ProducesProblem(StatusCodes.Status400BadRequest);
        Routes.ProducesProblem(StatusCodes.Status404NotFound);
        Routes.WithMetadata(new BehaviorRestGroupMetadata(
            ModuleDescriptor.Id,
            ModuleDescriptor.DisplayName,
            ModuleDescriptor.Description,
            ModuleDescriptor.Version,
            ModuleVersionMajor,
            BehaviorXmlDocumentation.GetSummary(module.GetType()),
            BehaviorXmlDocumentation.GetRemarks(module.GetType())));
    }

    /// <summary>
    /// Gets the underlying Minimal API route group.
    /// </summary>
    public RouteGroupBuilder Routes { get; }

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
    public string TagName { get; }

    /// <summary>
    /// Gets the major version parsed from the module descriptor when available.
    /// </summary>
    public int? ModuleVersionMajor { get; }

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
        ((IEndpointConventionBuilder)Routes).Add(convention);
    }

    private RouteHandlerBuilder MapBehaviorCore<TBehavior>(
        MethodInfo coreMethod,
        string pattern,
        Action<RouteHandlerBuilder>? configure)
        where TBehavior : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var contract = BehaviorRestEndpointContract.Create(typeof(TBehavior), ModuleDescriptor, TagName, ModuleVersionMajor);
        var closedMethod = coreMethod.MakeGenericMethod(typeof(TBehavior), contract.InputType, contract.OutputType);
        var builder = (RouteHandlerBuilder)closedMethod.Invoke(null, [this, pattern, contract])!;
        configure?.Invoke(builder);
        return builder;
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
        builder.WithTags(contract.TagName);
        builder.WithSummary(contract.Summary);
        builder.WithDescription(contract.Description);
        builder.WithMetadata(new BehaviorRestEndpointMetadata(
            contract.ModuleId,
            contract.ModuleVersion,
            contract.ModuleVersionMajor,
            contract.BehaviorId,
            contract.OperationName,
            contract.Summary,
            contract.Description,
            contract.TagName));
        builder.Produces<TOutput>(StatusCodes.Status200OK);
        builder.ProducesProblem(StatusCodes.Status400BadRequest);
        builder.Produces(StatusCodes.Status404NotFound);

        if (acceptsBody)
        {
            builder.Accepts(typeof(TInput), "application/json");
        }

        return builder;
    }

    private static async Task<Results<Ok<TOutput>, NoContent, BadRequest<ProblemDetails>, NotFound>> InvokeWithoutBodyAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        return await InvokeAsync<TBehavior, TInput, TOutput>(context, dispatcher, acceptsBody: false).ConfigureAwait(false);
    }

    private static async Task<Results<Ok<TOutput>, NoContent, BadRequest<ProblemDetails>, NotFound>> InvokeWithBodyAsync<TBehavior, TInput, TOutput>(
        HttpContext context,
        BehaviorDispatcher dispatcher)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
    {
        return await InvokeAsync<TBehavior, TInput, TOutput>(context, dispatcher, acceptsBody: true).ConfigureAwait(false);
    }

    private static async Task<Results<Ok<TOutput>, NoContent, BadRequest<ProblemDetails>, NotFound>> InvokeAsync<TBehavior, TInput, TOutput>(
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

            return result is null
                ? TypedResults.NoContent()
                : TypedResults.Ok((TOutput)result);
        }
        catch (BehaviorNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (JsonException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static ProblemDetails CreateProblemDetails(string detail)
    {
        return new ProblemDetails
        {
            Title = "Behavior request rejected.",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        };
    }

    private static int? ResolveModuleMajorVersion(string? moduleVersion)
    {
        return Version.TryParse(moduleVersion, out var parsedVersion)
            ? parsedVersion.Major
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
        string? Remarks);

    private sealed record BehaviorRestEndpointMetadata(
        string ModuleId,
        string? ModuleVersion,
        int? ModuleVersionMajor,
        string BehaviorId,
        string OperationName,
        string? Summary,
        string? Description,
        string TagName);

    private sealed record BehaviorRestEndpointContract(
        string ModuleId,
        string? ModuleVersion,
        int? ModuleVersionMajor,
        string BehaviorId,
        string OperationName,
        string TagName,
        string Summary,
        string Description,
        Type InputType,
        Type OutputType)
    {
        internal static BehaviorRestEndpointContract Create(
            Type behaviorType,
            ModuleDescriptor moduleDescriptor,
            string tagName,
            int? moduleVersionMajor)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);
            ArgumentNullException.ThrowIfNull(moduleDescriptor);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);

            var contractInterface = behaviorType.GetInterfaces()
                .FirstOrDefault(static candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
                ?? throw new InvalidOperationException(
                    $"Behavior type '{behaviorType.FullName}' does not implement IAppBehavior<TInput, TOutput>.");
            var typeArguments = contractInterface.GetGenericArguments();
            var behaviorId = GetBehaviorId(behaviorType);
            var summary = BehaviorXmlDocumentation.GetSummary(behaviorType)
                ?? behaviorId;
            var description = BehaviorXmlDocumentation.GetCombinedDescription(behaviorType)
                ?? moduleDescriptor.Description;
            var operationName = BuildOperationName(moduleDescriptor.Id, moduleVersionMajor, behaviorId);

            return new BehaviorRestEndpointContract(
                moduleDescriptor.Id,
                moduleDescriptor.Version,
                moduleVersionMajor,
                behaviorId,
                operationName,
                tagName,
                summary,
                description,
                typeArguments[0],
                typeArguments[1]);
        }

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
    }
}
