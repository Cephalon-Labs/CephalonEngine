using Cephalon.Abstractions.Behaviors;
using Microsoft.AspNetCore.Http;

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
    /// <returns>A populated context instance.</returns>
    internal static DefaultBehaviorContext From(HttpContext ctx, string behaviorId)
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
        var userId = ctx.User.FindFirst("sub")?.Value;
        var traceId = ctx.TraceIdentifier;

        if (correlationId is not null) metadata["CorrelationId"] = correlationId;
        if (tenantId is not null) metadata["TenantId"] = tenantId;
        if (userId is not null) metadata["UserId"] = userId;
        if (traceId is not null) metadata["TraceId"] = traceId;

        return new DefaultBehaviorContext
        {
            BehaviorId = behaviorId,
            Metadata = metadata,
            CancellationToken = ctx.RequestAborted,
            _isDirect = true
        };
    }

    /// <inheritdoc />
    public string BehaviorId { get; private init; } = string.Empty;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata { get; private init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
