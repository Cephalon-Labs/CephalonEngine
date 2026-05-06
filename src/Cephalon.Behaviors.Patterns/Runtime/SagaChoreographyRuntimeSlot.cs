using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Patterns.Runtime;

/// <summary>
/// Describes the compile-time runtime contract for one saga-choreography behavior.
/// </summary>
/// <remarks>
/// Source generation or explicit host registration should provide one slot for each
/// saga-choreography behavior whose runtime catalog metadata must be projected. The slot keeps
/// catalog projection away from runtime interface-shape reflection while preserving the closed
/// behavior, input, and result types.
/// </remarks>
public sealed class SagaChoreographyRuntimeSlot
{
    private SagaChoreographyRuntimeSlot(
        Type behaviorType,
        Type inputType,
        Type resultType,
        string authoringModel,
        string publicationResultShape,
        string? localOutputType)
    {
        BehaviorType = behaviorType;
        InputType = inputType;
        ResultType = resultType;
        AuthoringModel = NormalizeRequired(authoringModel, nameof(authoringModel));
        PublicationResultShape = NormalizeRequired(publicationResultShape, nameof(publicationResultShape));
        LocalOutputType = NormalizeOptional(localOutputType);
    }

    internal Type BehaviorType { get; }

    internal Type InputType { get; }

    internal Type ResultType { get; }

    internal string AuthoringModel { get; }

    internal string PublicationResultShape { get; }

    internal string? LocalOutputType { get; }

    /// <summary>
    /// Creates a saga-choreography runtime slot for a behavior whose closed generic behavior
    /// contract is known at compile time.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete saga-choreography behavior type.</typeparam>
    /// <typeparam name="TInput">The choreography input type.</typeparam>
    /// <typeparam name="TResult">The choreography result-contract type.</typeparam>
    /// <param name="authoringModel">
    /// The authored choreography model, such as <c>behavior</c> or <c>reactor</c>.
    /// </param>
    /// <param name="publicationResultShape">
    /// The publication result shape projected into runtime metadata.
    /// </param>
    /// <param name="localOutputType">
    /// The optional local-output type name carried by a typed choreography step result.
    /// </param>
    /// <returns>A runtime slot for the closed saga-choreography behavior contract.</returns>
    public static SagaChoreographyRuntimeSlot For<TBehavior, TInput, TResult>(
        string authoringModel,
        string publicationResultShape,
        string? localOutputType = null)
        where TBehavior : class, IAppBehavior<TInput, TResult>
    {
        return new SagaChoreographyRuntimeSlot(
            typeof(TBehavior),
            typeof(TInput),
            typeof(TResult),
            authoringModel,
            publicationResultShape,
            localOutputType);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
