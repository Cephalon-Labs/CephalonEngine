namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Defines stable metadata keys used by REST endpoint runtime descriptors.
/// </summary>
/// <remarks>
/// These keys appear in <see cref="RestEndpointRuntimeDescriptor.Metadata"/> as additive
/// operator-facing context. First-class descriptor properties remain authoritative for core
/// endpoint shape, while metadata clarifies compatibility and ownership details that should stay
/// inspectable without parsing host-specific endpoint metadata.
/// </remarks>
public static class RestEndpointRuntimeMetadataKeys
{
    /// <summary>
    /// Identifies the HTTP method that was resolved for the runtime endpoint.
    /// </summary>
    public const string Method = "method";

    /// <summary>
    /// Identifies the normalized authoring style that produced the runtime endpoint.
    /// </summary>
    public const string AuthoringStyle = "authoringStyle";

    /// <summary>
    /// Identifies the concrete behavior implementation type behind a behavior-backed REST endpoint.
    /// </summary>
    public const string BehaviorType = "behaviorType";

    /// <summary>
    /// Identifies the resolved route-group prefix for grouped REST endpoint publication.
    /// </summary>
    public const string RouteGroupPrefix = "routeGroupPrefix";

    /// <summary>
    /// Identifies the route pattern relative to the grouped REST endpoint publication boundary.
    /// </summary>
    public const string RelativePattern = "relativePattern";

    /// <summary>
    /// Identifies the stable source identity behind the runtime endpoint.
    /// </summary>
    public const string SourceId = "sourceId";

    /// <summary>
    /// Identifies the stable wire name for a preserved request-binding fallback mode.
    /// </summary>
    public const string BindingFallbackMode = "bindingFallbackMode";

    /// <summary>
    /// Identifies the required Cephalon capability key enforced at the REST boundary.
    /// </summary>
    public const string RequiredCapabilityKey = "requiredCapabilityKey";

    /// <summary>
    /// Identifies the comma-separated required Cephalon feature flags enforced at the REST boundary.
    /// </summary>
    public const string RequiredFeatureFlagIds = "requiredFeatureFlagIds";

    /// <summary>
    /// Identifies who owns the decision to activate the public REST publication path.
    /// </summary>
    public const string RestPublicationActivationOwnership = "restPublicationActivationOwnership";

    /// <summary>
    /// Identifies who owns the ASP.NET Core route materialization, runtime catalog, and governance reconciliation path.
    /// </summary>
    public const string RestMaterializationOwnership = "restMaterializationOwnership";

    /// <summary>
    /// Identifies who owns behavior-authored REST profile metadata consumed by low-code shorthand.
    /// </summary>
    public const string RestProfileMetadataOwnership = "restProfileMetadataOwnership";

    /// <summary>
    /// Identifies how the public REST publication path was explicitly activated.
    /// </summary>
    public const string RestPublicationActivationMode = "restPublicationActivationMode";
}
