using System.Text.Json;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Trust;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Exposes aggregated system projections for the showcase console UI.
/// </summary>
public sealed class ShowcaseSystemModule : ModuleBase, IEndpointModule
{
    private const string ReadCapabilityKey = "showcase.system.read";
    private const string ResetCapabilityKey = "showcase.system.reset";
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.system",
        displayName: "Showcase System",
        description: "System and operator-facing projection module for the showcase console.",
        tags: ["showcase", "system", "operations", "console"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: ReadCapabilityKey,
            displayName: "Showcase system read",
            description: "Read access to aggregated showcase runtime and business projections."));
        capabilities.Add(new Capability(
            key: ResetCapabilityKey,
            displayName: "Showcase system reset",
            description: "Reset the showcase sample back to its deterministic seeded state."));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/system");
        var routes = group.Routes;

        routes.MapGet("/summary", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await projections.GetSummaryAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseSystemSummary")
            .WithSummary("Get the operator-focused showcase summary.")
            .WithDescription("Returns a single operator-facing summary that blends runtime health, business KPIs, dependency health, and documentation links for the showcase console.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/runtime", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await projections.GetRuntimeAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseRuntimeProjection")
            .WithSummary("Get the runtime-focused showcase projection.")
            .WithDescription("Returns runtime and engine-facing detail such as modules, capabilities, active patterns, selected transports, and dependency health for the showcase console.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/governance", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await projections.GetGovernanceAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseGovernanceProjection")
            .WithSummary("Get the governance and policy projection.")
            .WithDescription("Returns package policy, trust defaults, capability governance, authorization policies, technology surfaces, and runtime-story summaries for the showcase console.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/business", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await projections.GetBusinessAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseBusinessProjection")
            .WithSummary("Get the business workload projection.")
            .WithDescription("Returns aggregated product, cart, order, inventory, and shipment views so the showcase UI can render workload state without browser-side fan-out.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/database-topology", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await projections.GetDatabaseTopologyAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseDatabaseTopologyProjection")
            .WithSummary("Get the database-topology and read-model sync projection.")
            .WithDescription("Returns the engine-owned database role and migration catalogs plus showcase read-model sync lag and durable projection-job state for the operator console.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/database-topology/brief", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Text(
                        await projections.GetDatabaseTopologyBriefAsync(cancellationToken).ConfigureAwait(false),
                        "text/markdown; charset=utf-8");
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseDatabaseTopologyBrief")
            .WithSummary("Get the shareable database-topology operator brief.")
            .WithDescription("Returns a Markdown operator brief derived from the live showcase database-topology projection, including readiness, next actions, and drill-down routes.")
            .Produces<string>(StatusCodes.Status200OK, "text/markdown")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/database-topology/handoff", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                if (denied is not null)
                {
                    return denied;
                }

                var handoff = await projections.GetDatabaseTopologyHandoffAsync(cancellationToken).ConfigureAwait(false);
                return Results.File(handoff.Bytes, handoff.ContentType, handoff.FileName);
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseDatabaseTopologyHandoff")
            .WithSummary("Download the database-topology operator handoff package.")
            .WithDescription("Returns a zip package that bundles a package README, the Markdown operator brief, a machine-readable handoff manifest, and the raw showcase database-topology projection so operators can share one artifact without losing route context or source data.")
            .Produces(StatusCodes.Status200OK, contentType: "application/zip")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/transports", async (
                HttpContext httpContext,
                ShowcaseSystemProjectionService projections,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await projections.GetTransportsAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseTransportProjection")
            .WithSummary("Get the transport and behavior route matrix.")
            .WithDescription("Returns the behavior-to-transport matrix plus the public REST operation catalog used by the showcase transport explorer.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/activity", (HttpContext httpContext, int? limit, ShowcaseSystemProjectionService projections) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(projections.GetActivity(limit ?? 60));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseActivityFeed")
            .WithSummary("Get recent showcase server activity.")
            .WithDescription("Returns recent server-backed request activity captured by the showcase host for live console rendering.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapGet("/activity/stream", async (
                HttpContext httpContext,
                ShowcaseActivityFeed activityFeed,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ReadCapabilityKey);
                return denied is not null
                    ? denied
                    : await StreamActivityAsync(httpContext, activityFeed, cancellationToken).ConfigureAwait(false);
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("StreamShowcaseActivity")
            .WithSummary("Stream showcase server activity.")
            .WithDescription("Streams new showcase activity entries as server-sent events for the live operator console.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routes.MapPost("/reset", async (
                HttpContext httpContext,
                ShowcaseResetService resetService,
                CancellationToken cancellationToken) =>
            {
                var denied = TryRequireCapability(httpContext, ResetCapabilityKey);
                return denied is not null
                    ? denied
                    : Results.Ok(await resetService.ResetAsync(cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ResetCapabilityKey)
            .WithName("ResetShowcaseSystemState")
            .WithSummary("Reset the showcase sample.")
            .WithDescription("Clears dynamic showcase state, reseeds deterministic reference data, and resets the in-memory event store so scenarios can be rerun cleanly.")
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> StreamActivityAsync(
        HttpContext httpContext,
        ShowcaseActivityFeed activityFeed,
        CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            httpContext.RequestAborted);
        var streamCancellationToken = linkedCancellation.Token;

        try
        {
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers["X-Accel-Buffering"] = "no";
            httpContext.Response.ContentType = "text/event-stream";

            var reader = activityFeed.Subscribe(streamCancellationToken);
            var readyPayload = JsonSerializer.Serialize(new
            {
                connectedAtUtc = DateTimeOffset.UtcNow,
                totalRecorded = activityFeed.TotalRecorded
            });
            await httpContext.Response.WriteAsync($"event: ready\ndata: {readyPayload}\n\n", streamCancellationToken).ConfigureAwait(false);
            await httpContext.Response.Body.FlushAsync(streamCancellationToken).ConfigureAwait(false);

            await foreach (var entry in reader.ReadAllAsync(streamCancellationToken).ConfigureAwait(false))
            {
                var payload = JsonSerializer.Serialize(entry, SseJsonOptions);
                await httpContext.Response.WriteAsync($"event: activity\ndata: {payload}\n\n", streamCancellationToken).ConfigureAwait(false);
                await httpContext.Response.Body.FlushAsync(streamCancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (
            streamCancellationToken.IsCancellationRequested ||
            httpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected or the request was aborted. Treat this as a normal SSE shutdown.
        }
        catch (IOException) when (
            streamCancellationToken.IsCancellationRequested ||
            httpContext.RequestAborted.IsCancellationRequested)
        {
            // Some transports surface client disconnects as IO failures after headers have already been written.
        }

        return Results.Empty;
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult? TryRequireCapability(
        HttpContext httpContext,
        string capabilityKey)
    {
        var evaluator = httpContext.RequestServices.GetRequiredService<CapabilityPolicyEvaluator>();
        var decision = evaluator.TryGetDecision(capabilityKey, out var resolvedDecision)
            ? resolvedDecision
            : throw new InvalidOperationException(
                $"Capability policy decision for '{capabilityKey}' was not available.");

        if (decision.IsAllowed)
        {
            return null;
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Capability access denied",
            detail: decision.Reason,
            extensions: new Dictionary<string, object?>
            {
                ["capabilityKey"] = decision.CapabilityKey,
                ["access"] = decision.Access.ToString(),
                ["sourceModuleId"] = decision.SourceModuleId,
                ["sourcePackageId"] = decision.SourcePackageId
            });
    }
}
