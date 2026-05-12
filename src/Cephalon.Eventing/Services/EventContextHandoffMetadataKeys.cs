namespace Cephalon.Eventing.Services;

/// <summary>
/// Provides stable metadata keys used when Cephalon stages validated event context into an outbox handoff.
/// </summary>
public static class EventContextHandoffMetadataKeys
{
    /// <summary>
    /// Identifies how event context was handed off to the next runtime boundary.
    /// </summary>
    public const string ContextHandoff = "cephalon.context.handoff";

    /// <summary>
    /// Identifies the validation posture used before the handoff was staged.
    /// </summary>
    public const string ContextValidation = "cephalon.context.validation";

    /// <summary>
    /// Lists the required context header names declared by the active policy set.
    /// </summary>
    public const string RequiredHeaders = "cephalon.context.requiredHeaders";

    /// <summary>
    /// Lists the required context header names present on the staged publication.
    /// </summary>
    public const string PresentHeaders = "cephalon.context.presentHeaders";

    /// <summary>
    /// Records the number of required context headers declared by the active policy set.
    /// </summary>
    public const string RequiredHeaderCount = "cephalon.context.requiredHeaderCount";

    /// <summary>
    /// Records the number of required context headers present on the staged publication.
    /// </summary>
    public const string PresentHeaderCount = "cephalon.context.presentHeaderCount";

    /// <summary>
    /// Records how tenant context was represented on the staged publication.
    /// </summary>
    public const string TenantContextPropagation = "cephalon.context.tenantPropagation";

    /// <summary>
    /// Records how correlation context was represented on the staged publication.
    /// </summary>
    public const string CorrelationContextPropagation = "cephalon.context.correlationPropagation";

    /// <summary>
    /// Records how causation context was represented on the staged publication.
    /// </summary>
    public const string CausationContextPropagation = "cephalon.context.causationPropagation";

    /// <summary>
    /// Records how baggage context was represented on the staged publication.
    /// </summary>
    public const string BaggageContextPropagation = "cephalon.context.baggagePropagation";

    /// <summary>
    /// Records whether the context handoff required the optional Wolverine companion.
    /// </summary>
    public const string WolverineRequired = "cephalon.context.wolverineRequired";
}
