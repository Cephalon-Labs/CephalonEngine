using System.Globalization;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Authorization;
using Cephalon.Identity.Configuration;
using Cephalon.Identity.Policies;
using Microsoft.Extensions.Logging;

namespace Cephalon.Identity.Services;

internal sealed class MetadataDrivenAuthorizationEvaluator(
    IAuthorizationPolicyCatalog policyCatalog,
    AppProfile appProfile,
    IdentityRuntimeOptions options,
    ILogger<MetadataDrivenAuthorizationEvaluator> logger) : IAuthorizationEvaluator
{
    public ValueTask<AuthorizationDecision> EvaluateAsync(
        AuthorizationSubject subject,
        AuthorizationResource resource,
        AuthorizationContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestedPolicyId = Normalize(context.PolicyId);
        if (requestedPolicyId is null)
        {
            return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                subject,
                context,
                policyId: null,
                reason: options.RequireExplicitPolicy
                    ? "No authorization policy id was provided."
                    : "No authorization policy id was provided and the default identity evaluator does not infer an implicit policy.",
                modes: ResolveConfiguredModes()));
        }

        var policy = policyCatalog.GetById(requestedPolicyId);
        if (policy is null)
        {
            return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                subject,
                context,
                requestedPolicyId,
                "The requested authorization policy is not active in the current runtime.",
                ResolveConfiguredModes()));
        }

        var modes = ResolveModes(policy);
        var ruleCount = 0;

        if (policy.Metadata.TryGetValue(IdentityPolicyMetadataKeys.RequiredRoles, out var requiredRolesValue))
        {
            var requiredRoles = ParseDelimitedValues(requiredRolesValue);
            if (requiredRoles.Length == 0)
            {
                return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                    subject,
                    context,
                    policy.Id,
                    "The authorization policy declared a required-roles rule without any roles.",
                    modes,
                    ruleCount));
            }

            ruleCount++;
            var roleMatch = policy.Metadata.TryGetValue(IdentityPolicyMetadataKeys.RequiredRoleMatch, out var matchValue) &&
                string.Equals(matchValue?.Trim(), IdentityPolicyMetadataKeys.RequiredRoleMatchAll, StringComparison.OrdinalIgnoreCase)
                ? IdentityPolicyMetadataKeys.RequiredRoleMatchAll
                : IdentityPolicyMetadataKeys.RequiredRoleMatchAny;
            var matched = MatchRoles(subject, requiredRoles, roleMatch);
            if (!matched)
            {
                return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                    subject,
                    context,
                    policy.Id,
                    $"Required role match '{roleMatch}' was not satisfied.",
                    modes,
                    ruleCount));
            }
        }

        var subjectAttributeFailure = EvaluateAttributeRules(
            policy.Metadata,
            IdentityPolicyMetadataKeys.SubjectAttributePrefix,
            key => ResolveSubjectValue(subject, key),
            ref ruleCount);
        if (subjectAttributeFailure is not null)
        {
            return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                subject,
                context,
                policy.Id,
                subjectAttributeFailure,
                modes,
                ruleCount));
        }

        var resourceAttributeFailure = EvaluateAttributeRules(
            policy.Metadata,
            IdentityPolicyMetadataKeys.ResourceAttributePrefix,
            key => ResolveResourceValue(resource, key),
            ref ruleCount);
        if (resourceAttributeFailure is not null)
        {
            return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                subject,
                context,
                policy.Id,
                resourceAttributeFailure,
                modes,
                ruleCount));
        }

        var contextAttributeFailure = EvaluateAttributeRules(
            policy.Metadata,
            IdentityPolicyMetadataKeys.ContextAttributePrefix,
            key => ResolveContextValue(context, key),
            ref ruleCount);
        if (contextAttributeFailure is not null)
        {
            return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                subject,
                context,
                policy.Id,
                contextAttributeFailure,
                modes,
                ruleCount));
        }

        if (ParseBoolean(policy.Metadata, IdentityPolicyMetadataKeys.RequireOwner))
        {
            ruleCount++;
            if (string.IsNullOrWhiteSpace(resource.OwnerSubjectId) ||
                !string.Equals(resource.OwnerSubjectId, subject.SubjectId, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                    subject,
                    context,
                    policy.Id,
                    "The authorization policy requires the current subject to own the resource.",
                    modes,
                    ruleCount));
            }
        }

        if (ParseBoolean(policy.Metadata, IdentityPolicyMetadataKeys.RequireTenantMatch))
        {
            ruleCount++;
            var resourceTenantId = Normalize(resource.TenantId);
            var contextTenantId = Normalize(context.TenantId);
            if (resourceTenantId is null ||
                contextTenantId is null ||
                !string.Equals(resourceTenantId, contextTenantId, StringComparison.OrdinalIgnoreCase) ||
                !subject.TenantIds.Contains(resourceTenantId, StringComparer.OrdinalIgnoreCase))
            {
                return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                    subject,
                    context,
                    policy.Id,
                    "The authorization policy requires the subject, resource, and context to stay within the same tenant boundary.",
                    modes,
                    ruleCount));
            }
        }

        if (ruleCount == 0)
        {
            return new ValueTask<AuthorizationDecision>(CreateDeniedDecision(
                subject,
                context,
                policy.Id,
                "The authorization policy does not declare any metadata-driven rules for the built-in evaluator.",
                modes,
                ruleCount));
        }

        var metadata = CreateDecisionMetadata(policy.Id, modes, ruleCount, outcome: "allowed");
        IdentityLoggerMessages.AuthorizationAllowed(logger, policy.Id, subject.SubjectId, context.Action, null);
        return new ValueTask<AuthorizationDecision>(AuthorizationDecision.Allow(
            policy.Id,
            "The authorization policy requirements were satisfied.",
            modes,
            metadata));
    }

    private AuthorizationDecision CreateDeniedDecision(
        AuthorizationSubject subject,
        AuthorizationContext context,
        string? policyId,
        string reason,
        IReadOnlyList<AuthorizationMode> modes,
        int ruleCount = 0)
    {
        var metadata = CreateDecisionMetadata(policyId, modes, ruleCount, outcome: "denied");
        IdentityLoggerMessages.AuthorizationDenied(logger, policyId ?? "none", subject.SubjectId, context.Action, null);
        return AuthorizationDecision.Deny(policyId, reason, modes, metadata);
    }

    private static string? EvaluateAttributeRules(
        IReadOnlyDictionary<string, string> metadata,
        string prefix,
        Func<string, string?> valueResolver,
        ref int ruleCount)
    {
        foreach (var rule in metadata
                     .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var attributeKey = rule.Key[prefix.Length..];
            if (string.IsNullOrWhiteSpace(attributeKey))
            {
                continue;
            }

            ruleCount++;
            var actualValue = valueResolver(attributeKey);
            var expectedValue = Normalize(rule.Value);
            if (expectedValue is null)
            {
                return $"The authorization policy declared an empty required value for '{rule.Key}'.";
            }

            if (!string.Equals(Normalize(actualValue), expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return $"The required value for '{rule.Key}' was not satisfied.";
            }
        }

        return null;
    }

    private IReadOnlyList<AuthorizationMode> ResolveModes(AuthorizationPolicyDescriptor policy)
    {
        if (policy.Modes.Count > 0)
        {
            return policy.Modes;
        }

        return ResolveConfiguredModes();
    }

    private AuthorizationMode[] ResolveConfiguredModes()
    {
        return appProfile.Identity.AuthorizationModes
            .Select(static mode => mode.Trim())
            .Where(static mode => !string.IsNullOrWhiteSpace(mode))
            .Select(ParseAuthorizationMode)
            .Where(static mode => mode is not null)
            .Select(static mode => mode!.Value)
            .Distinct()
            .OrderBy(static mode => mode)
            .ToArray();
    }

    private static AuthorizationMode? ParseAuthorizationMode(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "RBAC" => AuthorizationMode.Rbac,
            "ABAC" => AuthorizationMode.Abac,
            "POLICY" => AuthorizationMode.Policy,
            _ => null
        };
    }

    private static bool MatchRoles(
        AuthorizationSubject subject,
        IReadOnlyList<string> requiredRoles,
        string roleMatch)
    {
        var subjectRoles = subject.Roles
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return string.Equals(roleMatch, IdentityPolicyMetadataKeys.RequiredRoleMatchAll, StringComparison.OrdinalIgnoreCase)
            ? requiredRoles.All(subjectRoles.Contains)
            : requiredRoles.Any(subjectRoles.Contains);
    }

    private static string[] ParseDelimitedValues(string? value)
    {
        return value?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static Dictionary<string, string> CreateDecisionMetadata(
        string? policyId,
        IReadOnlyList<AuthorizationMode> modes,
        int ruleCount,
        string outcome)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["evaluator"] = "metadata-driven",
            ["policyId"] = policyId ?? "none",
            ["modeCount"] = modes.Count.ToString(CultureInfo.InvariantCulture),
            ["modes"] = modes.Count == 0
                ? "none"
                : string.Join(",", modes.Select(ToMetadataValue)),
            ["ruleCount"] = ruleCount.ToString(CultureInfo.InvariantCulture),
            ["outcome"] = outcome
        };
    }

    private static string ToMetadataValue(AuthorizationMode mode)
    {
        return mode switch
        {
            AuthorizationMode.Rbac => "rbac",
            AuthorizationMode.Abac => "abac",
            AuthorizationMode.Policy => "policy",
            _ => mode.ToString().ToLowerInvariant()
        };
    }

    private static bool ParseBoolean(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        return metadata.TryGetValue(key, out var value) &&
            bool.TryParse(value?.Trim(), out var parsed) &&
            parsed;
    }

    private static string? ResolveSubjectValue(AuthorizationSubject subject, string key)
    {
        return key.Trim().ToLowerInvariant() switch
        {
            "subjectid" => subject.SubjectId,
            "displayname" => subject.DisplayName,
            "tenantids" => subject.TenantIds.Count == 0 ? null : string.Join(",", subject.TenantIds),
            _ => subject.Attributes.TryGetValue(key, out var value) ? value : null
        };
    }

    private static string? ResolveResourceValue(AuthorizationResource resource, string key)
    {
        return key.Trim().ToLowerInvariant() switch
        {
            "resourcetype" => resource.ResourceType,
            "resourceid" => resource.ResourceId,
            "tenantid" => resource.TenantId,
            "ownersubjectid" => resource.OwnerSubjectId,
            _ => resource.Attributes.TryGetValue(key, out var value) ? value : null
        };
    }

    private static string? ResolveContextValue(AuthorizationContext context, string key)
    {
        return key.Trim().ToLowerInvariant() switch
        {
            "action" => context.Action,
            "policyid" => context.PolicyId,
            "tenantid" => context.TenantId,
            "correlationid" => context.CorrelationId,
            _ => context.Attributes.TryGetValue(key, out var value) ? value : null
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

internal static class IdentityLoggerMessages
{
    private static readonly Action<ILogger, string, string, string, Exception?> AuthorizationAllowedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(IdentityDiagnosticsConventions.AuthorizationAllowed.Id, IdentityDiagnosticsConventions.AuthorizationAllowed.Name),
            IdentityDiagnosticsConventions.AuthorizationAllowed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, Exception?> AuthorizationDeniedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(IdentityDiagnosticsConventions.AuthorizationDenied.Id, IdentityDiagnosticsConventions.AuthorizationDenied.Name),
            IdentityDiagnosticsConventions.AuthorizationDenied.MessageTemplate);

    public static void AuthorizationAllowed(ILogger logger, string policyId, string subjectId, string action, Exception? exception)
    {
        AuthorizationAllowedMessage(logger, policyId, subjectId, action, exception);
    }

    public static void AuthorizationDenied(ILogger logger, string policyId, string subjectId, string action, Exception? exception)
    {
        AuthorizationDeniedMessage(logger, policyId, subjectId, action, exception);
    }
}
