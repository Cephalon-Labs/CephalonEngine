using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Behaviors.Http;

/// <summary>
/// Minimal <see cref="IBehaviorContext" /> implementation built from an
/// <see cref="HttpContext" />. Extracts correlation, tenant, and user identifiers
/// from standard HTTP headers and the claims principal.
/// </summary>
internal sealed class DefaultBehaviorContext : IBehaviorContext
{
    private DefaultBehaviorContext() { }

    /// <summary>
    /// Builds a <see cref="DefaultBehaviorContext" /> from the current <see cref="HttpContext" />.
    /// </summary>
    /// <param name="ctx">The active HTTP context.</param>
    /// <param name="behaviorId">The behavior identifier being dispatched.</param>
    /// <param name="transportId">The transport identifier that produced this behavior invocation when one is known.</param>
    /// <returns>A populated context instance.</returns>
    internal static DefaultBehaviorContext From(HttpContext ctx, string behaviorId, string? transportId = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(behaviorId);

        var metadata = ctx.Request.Headers
            .Where(h => h.Key.StartsWith("X-Meta-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                h => h.Key[7..],
                h => h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

        // Inject standard ambient values as metadata entries so callers can
        // access them uniformly through IBehaviorContext.Metadata.
        var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var tenantId = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.Identity?.Name;
        var traceId = ctx.TraceIdentifier;
        var environmentName = ctx.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName
            ?? ctx.RequestServices.GetService<IWebHostEnvironment>()?.EnvironmentName;

        if (correlationId is not null) metadata["CorrelationId"] = correlationId;
        if (tenantId is not null) metadata["TenantId"] = tenantId;
        if (userId is not null) metadata["UserId"] = userId;
        if (userId is not null) metadata["SubjectId"] = userId;
        if (traceId is not null) metadata["TraceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(environmentName)) metadata["EnvironmentName"] = environmentName.Trim();
        if (!string.IsNullOrWhiteSpace(transportId)) metadata["TransportId"] = transportId.Trim();

        return new DefaultBehaviorContext
        {
            BehaviorId = behaviorId,
            CorrelationId = correlationId,
            Metadata = metadata,
            EventStore = ctx.RequestServices.GetService<IEventStore>(),
            CancellationToken = ctx.RequestAborted,
            _isDirect = true
        };
    }

    /// <inheritdoc />
    public string BehaviorId { get; private init; } = string.Empty;

    /// <inheritdoc />
    public string? CorrelationId { get; private init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata { get; private init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IEventStore? EventStore { get; private init; }

    /// <summary>
    /// Gets the cancellation token from the underlying HTTP request.
    /// </summary>
    public CancellationToken CancellationToken { get; private init; }

    private bool _isDirect;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Always thrown when this context is used with the <c>direct</c> pattern.
    /// Use the behavior return value to communicate results instead.
    /// </exception>
    public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
    {
        if (_isDirect)
        {
            throw new NotSupportedException(
                "ReplyAsync is not supported in the 'direct' HTTP binding. " +
                "Use the behavior return value to communicate results.");
        }

        // Non-direct patterns (e.g. SSE / WebSocket) override this via subclassing or injection.
        return Task.CompletedTask;
    }
}
