namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Describes one field-level change captured by an audit entry.
/// </summary>
public sealed class AuditChange
{
    /// <summary>
    /// Creates a new audit change.
    /// </summary>
    /// <param name="fieldName">The logical field or property name that changed.</param>
    /// <param name="oldValue">The previous serialized value when one is known.</param>
    /// <param name="newValue">The new serialized value when one is known.</param>
    public AuditChange(
        string fieldName,
        string? oldValue = null,
        string? newValue = null)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Audit change field name is required.", nameof(fieldName));
        }

        FieldName = fieldName.Trim();
        OldValue = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim();
        NewValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
    }

    /// <summary>
    /// Gets the logical field or property name that changed.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the previous serialized value when one is known.
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// Gets the new serialized value when one is known.
    /// </summary>
    public string? NewValue { get; }
}
