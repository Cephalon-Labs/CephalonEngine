namespace Cephalon.Behaviors.Services;

/// <summary>
/// Describes the closed input, output, and response contract for one behavior implementation.
/// </summary>
/// <remarks>
/// The descriptor is intentionally transport-neutral. HTTP, messaging, and future adapters can
/// consume the same source-generated or explicitly registered metadata instead of resolving
/// <c>IAppBehavior&lt;TInput,TOutput&gt;</c> shape from runtime behavior types.
/// </remarks>
public sealed class BehaviorContractDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="BehaviorContractDescriptor" />.
    /// </summary>
    /// <param name="id">The stable behavior identifier.</param>
    /// <param name="behaviorType">The concrete behavior implementation type.</param>
    /// <param name="inputType">The closed behavior input type.</param>
    /// <param name="outputType">The closed behavior output type.</param>
    /// <param name="responseType">
    /// The payload type transports should document for successful responses.
    /// </param>
    /// <param name="returnsStructuredResult">
    /// Indicates whether <paramref name="outputType" /> is a structured behavior result such as
    /// <c>Result&lt;T&gt;</c> or <c>BehaviorResult&lt;T&gt;</c>.
    /// </param>
    /// <param name="inputIsScalar">
    /// Indicates whether the input type is scalar and therefore cannot host property-level bindings.
    /// </param>
    /// <param name="inputProperties">The public readable input properties when the input is an object.</param>
    public BehaviorContractDescriptor(
        string id,
        Type behaviorType,
        Type inputType,
        Type outputType,
        Type responseType,
        bool returnsStructuredResult,
        bool inputIsScalar,
        IReadOnlyList<BehaviorInputPropertyDescriptor>? inputProperties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        BehaviorType = behaviorType ?? throw new ArgumentNullException(nameof(behaviorType));
        InputType = inputType ?? throw new ArgumentNullException(nameof(inputType));
        OutputType = outputType ?? throw new ArgumentNullException(nameof(outputType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        ReturnsStructuredResult = returnsStructuredResult;
        InputIsScalar = inputIsScalar;
        InputProperties = inputProperties?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the concrete behavior implementation type.
    /// </summary>
    public Type BehaviorType { get; }

    /// <summary>
    /// Gets the closed behavior input type.
    /// </summary>
    public Type InputType { get; }

    /// <summary>
    /// Gets the closed behavior output type.
    /// </summary>
    public Type OutputType { get; }

    /// <summary>
    /// Gets the payload type transports should document for successful responses.
    /// </summary>
    public Type ResponseType { get; }

    /// <summary>
    /// Gets a value indicating whether the output is a structured behavior result wrapper.
    /// </summary>
    public bool ReturnsStructuredResult { get; }

    /// <summary>
    /// Gets a value indicating whether the input type is scalar.
    /// </summary>
    public bool InputIsScalar { get; }

    /// <summary>
    /// Gets the public readable input properties when the input is an object.
    /// </summary>
    public IReadOnlyList<BehaviorInputPropertyDescriptor> InputProperties { get; }
}
