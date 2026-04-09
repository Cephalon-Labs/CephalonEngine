namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Describes the logical public API surface projected by a behavior across transport adapters.
/// </summary>
/// <remarks>
/// This descriptor stays transport-agnostic. Route-shaped non-REST adapters such as JSON-RPC,
/// GraphQL-over-SSE, GraphQL-over-WebSocket, Server-Sent Events, and WebSocket can project
/// canonical routes from the same logical surface without forcing transport-specific path
/// details into behavior identifiers. Public REST stays module-owned.
/// </remarks>
public sealed class BehaviorApiSurfaceDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="BehaviorApiSurfaceDescriptor" />.
    /// </summary>
    /// <param name="groupPath">The logical group path, such as <c>cart</c> or <c>orders/status</c>.</param>
    /// <param name="operationPath">The logical operation path, such as <c>get</c> or <c>remove-item</c>.</param>
    public BehaviorApiSurfaceDescriptor(string groupPath, string operationPath)
    {
        GroupPath = NormalizePath(groupPath, allowEmpty: true);
        OperationPath = NormalizePath(operationPath, allowEmpty: false);
    }

    /// <summary>
    /// Gets the logical group path shared by transport-specific projections.
    /// </summary>
    public string GroupPath { get; }

    /// <summary>
    /// Gets the logical operation path shared by transport-specific projections.
    /// </summary>
    public string OperationPath { get; }

    /// <summary>
    /// Creates a default API surface descriptor from the supplied behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <returns>The default logical API surface derived from the identifier.</returns>
    /// <remarks>
    /// Behavior identifiers such as <c>cart.get</c> become group <c>cart</c> plus operation <c>get</c>.
    /// Identifiers with more than two segments join all but the final segment into the group path.
    /// </remarks>
    public static BehaviorApiSurfaceDescriptor CreateDefault(string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        var segments = behaviorId
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Behavior API surface defaults require at least one behavior-id segment.");
        }

        if (segments.Length == 1)
        {
            return new BehaviorApiSurfaceDescriptor(string.Empty, segments[0]);
        }

        var groupPath = string.Join("/", segments[..^1]);
        var operationPath = segments[^1];
        return new BehaviorApiSurfaceDescriptor(groupPath, operationPath);
    }

    private static string NormalizePath(string? value, bool allowEmpty)
    {
        var segments = (value ?? string.Empty)
            .Split(['/', '\\', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            if (allowEmpty)
            {
                return string.Empty;
            }

            throw new InvalidOperationException("Behavior API surface paths must contain at least one segment.");
        }

        return string.Join("/", segments);
    }
}
