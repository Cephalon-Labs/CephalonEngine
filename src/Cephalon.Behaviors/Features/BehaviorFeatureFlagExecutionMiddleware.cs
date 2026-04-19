using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Features;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Behaviors.Features;

internal sealed class BehaviorFeatureFlagExecutionMiddleware(
    IFeatureToggle featureToggle,
    IServiceProvider services)
    : IBehaviorExecutionMiddleware
{
    public ValueTask<object?> InvokeAsync(
        BehaviorExecutionInvocation invocation,
        BehaviorExecutionDelegate next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(next);

        if (invocation.Descriptor.RequiredFeatureFlagIds.Count == 0)
        {
            return next(invocation, cancellationToken);
        }

        var environmentName = services.GetService<IHostEnvironment>()?.EnvironmentName;
        var evaluationContext = BehaviorFeatureFlagEvaluationContextFactory.Create(
            invocation,
            environmentName);
        var rejectedEvaluation = invocation.Descriptor.RequiredFeatureFlagIds
            .Select(featureFlagId => featureToggle.Evaluate(featureFlagId, evaluationContext))
            .FirstOrDefault(static evaluation => !evaluation.IsEnabled);
        if (rejectedEvaluation is not null)
        {
            throw new BehaviorFeatureDisabledException(
                invocation.BehaviorId,
                rejectedEvaluation.FeatureId,
                invocation.Descriptor.RequiredFeatureFlagIds,
                rejectedEvaluation.Reason,
                rejectedEvaluation.SourceKind,
                rejectedEvaluation.SourceModuleId);
        }

        return next(invocation, cancellationToken);
    }
}
