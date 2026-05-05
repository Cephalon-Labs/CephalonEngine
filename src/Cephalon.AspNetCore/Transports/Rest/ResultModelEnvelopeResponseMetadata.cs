namespace Cephalon.AspNetCore.Transports.Rest;

/// <summary>
/// Describes a response whose OpenAPI schema should be published through the Cephalon result envelope.
/// </summary>
/// <remarks>
/// Runtime adapters can attach this metadata when the wire response uses <see cref="ResultModel{TModel}" />
/// but endpoint metadata should avoid constructing closed generic result-envelope types at runtime.
/// </remarks>
public sealed class ResultModelEnvelopeResponseMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultModelEnvelopeResponseMetadata" /> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code described by this response metadata.</param>
    /// <param name="payloadType">The payload type carried in the envelope <c>data</c> property.</param>
    /// <param name="isError">Whether the response represents an error envelope.</param>
    public ResultModelEnvelopeResponseMetadata(int statusCode, Type payloadType, bool isError = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(statusCode);
        ArgumentNullException.ThrowIfNull(payloadType);

        StatusCode = statusCode;
        PayloadType = payloadType;
        IsError = isError;
    }

    /// <summary>
    /// Gets the HTTP status code described by this response metadata.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the payload type carried in the envelope <c>data</c> property.
    /// </summary>
    public Type PayloadType { get; }

    /// <summary>
    /// Gets a value indicating whether the response represents an error envelope.
    /// </summary>
    public bool IsError { get; }
}
