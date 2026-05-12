using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventContextPolicyEvaluation
{
    private static readonly EventContextPolicyEvaluation Empty = new(
        policies: [],
        requiredHeaderNames: [],
        presentHeaderNames: [],
        missingHeaderNames: [],
        tenantContextPropagation: "not-declared",
        correlationContextPropagation: "not-declared",
        causationIdPropagation: "not-declared",
        baggagePropagation: "not-declared",
        messageHeaderPolicy: "not-declared");

    private EventContextPolicyEvaluation(
        IReadOnlyList<EventContextPolicyDescriptor> policies,
        IReadOnlyList<string> requiredHeaderNames,
        IReadOnlyList<string> presentHeaderNames,
        IReadOnlyList<string> missingHeaderNames,
        string tenantContextPropagation,
        string correlationContextPropagation,
        string causationIdPropagation,
        string baggagePropagation,
        string messageHeaderPolicy)
    {
        Policies = policies;
        RequiredHeaderNames = requiredHeaderNames;
        PresentHeaderNames = presentHeaderNames;
        MissingHeaderNames = missingHeaderNames;
        TenantContextPropagation = tenantContextPropagation;
        CorrelationContextPropagation = correlationContextPropagation;
        CausationIdPropagation = causationIdPropagation;
        BaggagePropagation = baggagePropagation;
        MessageHeaderPolicy = messageHeaderPolicy;
    }

    public IReadOnlyList<EventContextPolicyDescriptor> Policies { get; }

    public IReadOnlyList<string> RequiredHeaderNames { get; }

    public IReadOnlyList<string> PresentHeaderNames { get; }

    public IReadOnlyList<string> MissingHeaderNames { get; }

    public string TenantContextPropagation { get; }

    public string CorrelationContextPropagation { get; }

    public string CausationIdPropagation { get; }

    public string BaggagePropagation { get; }

    public string MessageHeaderPolicy { get; }

    public bool HasPolicies => Policies.Count > 0;

    public bool IsValid => MissingHeaderNames.Count == 0;

    public static EventContextPolicyEvaluation Evaluate(
        IEventContextPolicyCatalog catalog,
        EventPublication publication)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(publication);

        var policies = catalog.Policies;
        if (policies.Count == 0)
        {
            return Empty;
        }

        var requiredHeaderNames = policies
            .Where(static policy => policy.ValidatesMessageHeaders)
            .SelectMany(static policy => policy.HeaderNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static headerName => headerName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var presentHeaderNames = requiredHeaderNames
            .Where(publication.Headers.ContainsKey)
            .ToArray();
        var missingHeaderNames = requiredHeaderNames
            .Except(presentHeaderNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var messageHeaderPolicy = requiredHeaderNames.Length == 0
            ? policies.Any(static policy => policy.HeaderNames.Count > 0) ? "policy-declared" : "not-declared"
            : missingHeaderNames.Length == 0 ? "validated" : "validation-failed";

        return new EventContextPolicyEvaluation(
            policies,
            requiredHeaderNames,
            presentHeaderNames,
            missingHeaderNames,
            ResolveTenantContextPropagation(policies, publication),
            ResolveCorrelationContextPropagation(policies, publication),
            ResolveHeaderContextPropagation(
                policies.Any(static policy => policy.DeclaresCausationId),
                publication,
                EventContextHeaderNames.CausationId),
            ResolveHeaderContextPropagation(
                policies.Any(static policy => policy.DeclaresBaggage),
                publication,
                EventContextHeaderNames.Baggage),
            messageHeaderPolicy);
    }

    public string CreateValidationFailureMessage(EventPublication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Event context policy validation failed for publication '{publication.Id}'. Missing required headers: {string.Join(",", MissingHeaderNames)}.");
    }

    public void ApplyMetadata(IDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!HasPolicies)
        {
            return;
        }

        metadata["contextPolicyCatalog"] = "active";
        metadata["contextPolicyCount"] = Policies.Count.ToString(CultureInfo.InvariantCulture);
        metadata["contextPolicyIds"] = string.Join(",", Policies.Select(static policy => policy.Id));
        metadata["contextHeaderValidation"] = RequiredHeaderNames.Count == 0
            ? "not-required"
            : IsValid ? "validated" : "failed";
        metadata["contextRequiredHeaderCount"] = RequiredHeaderNames.Count.ToString(CultureInfo.InvariantCulture);
        metadata["contextPresentHeaderCount"] = PresentHeaderNames.Count.ToString(CultureInfo.InvariantCulture);
        metadata["contextMissingHeaderCount"] = MissingHeaderNames.Count.ToString(CultureInfo.InvariantCulture);
        metadata["contextRequiredHeaders"] = string.Join(",", RequiredHeaderNames);
        metadata["contextPresentHeaders"] = string.Join(",", PresentHeaderNames);
        metadata["contextMissingHeaders"] = string.Join(",", MissingHeaderNames);
        metadata["tenantContextPropagation"] = TenantContextPropagation;
        metadata["correlationContextPropagation"] = CorrelationContextPropagation;
        metadata["causationIdPropagation"] = CausationIdPropagation;
        metadata["baggagePropagation"] = BaggagePropagation;
        metadata["messageHeaderPolicy"] = MessageHeaderPolicy;
        metadata["executableContextPolicy"] = "publisher-enforced";
        metadata["wolverineRequired"] = "false";
    }

    public void ApplyOutboxHandoffMetadata(IDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (!HasPolicies)
        {
            return;
        }

        metadata[EventContextHandoffMetadataKeys.ContextHandoff] = "outbox-staged-headers";
        metadata[EventContextHandoffMetadataKeys.ContextValidation] = RequiredHeaderNames.Count == 0
            ? "not-required"
            : "publisher-enforced";
        metadata[EventContextHandoffMetadataKeys.RequiredHeaderCount] = RequiredHeaderNames.Count.ToString(CultureInfo.InvariantCulture);
        metadata[EventContextHandoffMetadataKeys.PresentHeaderCount] = PresentHeaderNames.Count.ToString(CultureInfo.InvariantCulture);
        metadata[EventContextHandoffMetadataKeys.RequiredHeaders] = string.Join(",", RequiredHeaderNames);
        metadata[EventContextHandoffMetadataKeys.PresentHeaders] = string.Join(",", PresentHeaderNames);
        metadata[EventContextHandoffMetadataKeys.TenantContextPropagation] = TenantContextPropagation;
        metadata[EventContextHandoffMetadataKeys.CorrelationContextPropagation] = CorrelationContextPropagation;
        metadata[EventContextHandoffMetadataKeys.CausationContextPropagation] = CausationIdPropagation;
        metadata[EventContextHandoffMetadataKeys.BaggageContextPropagation] = BaggagePropagation;
        metadata[EventContextHandoffMetadataKeys.WolverineRequired] = "false";
    }

    private static string ResolveTenantContextPropagation(
        IReadOnlyList<EventContextPolicyDescriptor> policies,
        EventPublication publication)
    {
        if (!policies.Any(static policy => policy.DeclaresTenantContext))
        {
            return "not-declared";
        }

        if (!string.IsNullOrWhiteSpace(publication.TenantId))
        {
            return "publication-field-forwarded";
        }

        return publication.Headers.ContainsKey(EventContextHeaderNames.TenantId)
            ? "header-forwarded"
            : "missing";
    }

    private static string ResolveCorrelationContextPropagation(
        IReadOnlyList<EventContextPolicyDescriptor> policies,
        EventPublication publication)
    {
        if (!policies.Any(static policy => policy.DeclaresCorrelationId))
        {
            return "not-declared";
        }

        if (!string.IsNullOrWhiteSpace(publication.CorrelationId))
        {
            return "publication-field-forwarded";
        }

        return publication.Headers.ContainsKey(EventContextHeaderNames.CorrelationId)
            ? "header-forwarded"
            : "missing";
    }

    private static string ResolveHeaderContextPropagation(
        bool declared,
        EventPublication publication,
        string headerName)
    {
        if (!declared)
        {
            return "not-declared";
        }

        return publication.Headers.ContainsKey(headerName)
            ? "header-forwarded"
            : "missing";
    }
}
