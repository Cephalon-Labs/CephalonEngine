namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Describes one explicit request-source binding for a behavior input property on a metadata-only
/// REST profile.
/// </summary>
/// <param name="PropertyName">The behavior input property that receives the bound value.</param>
/// <param name="Source">The HTTP request source that supplies the value.</param>
/// <param name="Name">
/// The external route key, query-string key, header name, or body property name to read from. When
/// <see langword="null" />, Cephalon falls back to <paramref name="PropertyName" />.
/// </param>
public sealed record BehaviorRestBindingDescriptor(
    string PropertyName,
    BehaviorRestBindingSource Source,
    string? Name = null);
