using System.Text.Json.Serialization;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes an operator action declared by the subsystem that owns a runtime introspection entry.
/// </summary>
/// <remarks>
/// This contract advertises an action without coupling the host-agnostic engine to an HTTP route or transport.
/// The owning subsystem remains responsible for authorization, approval, idempotency, execution, and audit behavior.
/// </remarks>
public sealed class RuntimeOperatorAction
{
    /// <summary>Creates an operator action declaration.</summary>
    /// <param name="id">The stable action identifier within the owning section.</param>
    /// <param name="displayName">The operator-facing action name.</param>
    /// <param name="description">A human-readable explanation of the action.</param>
    /// <param name="requiresApproval">Whether execution requires an explicit approval step.</param>
    [JsonConstructor]
    public RuntimeOperatorAction(
        string id,
        string displayName,
        string description,
        bool requiresApproval)
    {
        Id = RequireValue(id, nameof(id), "Action id is required.");
        DisplayName = RequireValue(displayName, nameof(displayName), "Action display name is required.");
        Description = RequireValue(description, nameof(description), "Action description is required.");
        RequiresApproval = requiresApproval;
    }

    /// <summary>Gets the stable action identifier within the owning section.</summary>
    public string Id { get; }

    /// <summary>Gets the operator-facing action name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the human-readable explanation of the action.</summary>
    public string Description { get; }

    /// <summary>Gets whether execution requires an explicit approval step.</summary>
    public bool RequiresApproval { get; }

    private static string RequireValue(string value, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }
}
