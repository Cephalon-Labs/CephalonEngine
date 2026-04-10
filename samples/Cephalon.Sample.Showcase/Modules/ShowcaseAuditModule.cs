using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Exposes a public showcase-facing audit-history read surface.
/// </summary>
public sealed class ShowcaseAuditModule : ModuleBase, IEndpointModule
{
    private const string ReadCapabilityKey = "showcase.audit.history.read";

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.audit",
        displayName: "Showcase Audit",
        description: "Audit-history query module for the showcase sample.",
        tags: ["showcase", "audit", "history", "operations"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: ReadCapabilityKey,
            displayName: "Audit history read",
            description: "Read access to the showcase audit-history surface."));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/audit");
        var routes = group.Routes;

        routes.MapGet("/history", async (
                string? category,
                string? action,
                string? subjectType,
                string? subjectId,
                string? actorId,
                string? tenantId,
                string? correlationId,
                string? outcome,
                DateTimeOffset? occurredFromUtc,
                DateTimeOffset? occurredToUtc,
                int? offset,
                int? limit,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var reader = httpContext.RequestServices.GetService<IAuditHistoryReader>();
                if (reader is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Audit history reader unavailable",
                        detail: "The showcase host is not currently configured with a queryable audit-history provider.");
                }

                if (!TryParseAuditOutcome(outcome, out var parsedOutcome))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid audit outcome filter",
                        detail: $"Audit outcome '{outcome}' is not supported.");
                }

                var query = new AuditHistoryQuery(
                    category: category,
                    action: action,
                    subjectType: subjectType,
                    subjectId: subjectId,
                    actorId: actorId,
                    tenantId: tenantId,
                    correlationId: correlationId,
                    outcome: parsedOutcome,
                    occurredFromUtc: occurredFromUtc,
                    occurredToUtc: occurredToUtc,
                    offset: offset ?? 0,
                    limit: limit ?? AuditHistoryQuery.DefaultLimit);

                return Results.Ok(await reader.QueryAsync(query, cancellationToken).ConfigureAwait(false));
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseAuditHistory")
            .WithSummary("Query showcase audit history.")
            .WithDescription("Returns paged audit-history entries from the configured durable audit-history reader when the showcase is running with a queryable audit-history provider.")
            .Produces<AuditHistoryQueryResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        routes.MapGet("/history/{auditEntryId}", async (
                string auditEntryId,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var reader = httpContext.RequestServices.GetService<IAuditHistoryReader>();
                if (reader is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Audit history reader unavailable",
                        detail: "The showcase host is not currently configured with a queryable audit-history provider.");
                }

                var entry = await reader.GetByIdAsync(auditEntryId, cancellationToken).ConfigureAwait(false);
                return entry is null
                    ? Results.NotFound()
                    : Results.Ok(entry);
            })
            .RequireCapability(ReadCapabilityKey)
            .WithName("GetShowcaseAuditHistoryEntry")
            .WithSummary("Resolve one showcase audit-history entry.")
            .WithDescription("Returns one audit-history entry by stable identifier when the showcase is running with a queryable audit-history provider.")
            .Produces<AuditHistoryEntry>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static bool TryParseAuditOutcome(
        string? value,
        out AuditOutcome? outcome)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            outcome = null;
            return true;
        }

        if (Enum.TryParse<AuditOutcome>(value.Trim(), ignoreCase: true, out var parsed))
        {
            outcome = parsed;
            return true;
        }

        outcome = null;
        return false;
    }
}
