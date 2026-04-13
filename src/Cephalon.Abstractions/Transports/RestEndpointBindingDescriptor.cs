namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one resolved request-binding rule for a public REST endpoint.
/// </summary>
public sealed class RestEndpointBindingDescriptor
{
    /// <summary>
    /// Creates a resolved REST endpoint binding descriptor.
    /// </summary>
    /// <param name="propertyName">The target request-model property name.</param>
    /// <param name="source">The HTTP request source that supplies the value.</param>
    /// <param name="name">The route/query/header/body member name when one is available.</param>
    public RestEndpointBindingDescriptor(
        string propertyName,
        RestEndpointBindingSource source,
        string? name = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("A non-empty property name is required.", nameof(propertyName));
        }

        if (!Enum.IsDefined(source) || source == RestEndpointBindingSource.Unspecified)
        {
            throw new ArgumentException("A supported binding source is required.", nameof(source));
        }

        PropertyName = propertyName.Trim();
        Source = source;
        Name = string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim();
    }

    /// <summary>
    /// Gets the target request-model property name.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the HTTP request source that supplies the value.
    /// </summary>
    public RestEndpointBindingSource Source { get; }

    /// <summary>
    /// Gets the route/query/header/body member name when one is available.
    /// </summary>
    public string? Name { get; }
}
