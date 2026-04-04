using System.Globalization;
using Cephalon.Abstractions.Authorization;
using Microsoft.Extensions.Logging;

namespace Cephalon.Identity.Services;

internal sealed class DisabledAuthorizationEvaluator(
    ILogger<DisabledAuthorizationEvaluator> logger) : IAuthorizationEvaluator
{
    public ValueTask<AuthorizationDecision> EvaluateAsync(
        AuthorizationSubject subject,
        AuthorizationResource resource,
        AuthorizationContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string reason = "The built-in Cephalon identity evaluator is disabled. Register a custom IAuthorizationEvaluator or re-enable Engine:Identity:EnableDefaultEvaluator.";
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["evaluator"] = "disabled",
            ["policyId"] = string.IsNullOrWhiteSpace(context.PolicyId) ? "none" : context.PolicyId!,
            ["modeCount"] = "0",
            ["modes"] = "none",
            ["ruleCount"] = "0",
            ["outcome"] = "denied"
        };

        IdentityLoggerMessages.AuthorizationDenied(
            logger,
            string.IsNullOrWhiteSpace(context.PolicyId) ? "none" : context.PolicyId!,
            subject.SubjectId,
            context.Action,
            null);
        return ValueTask.FromResult(AuthorizationDecision.Deny(
            context.PolicyId,
            reason,
            [],
            metadata));
    }
}
