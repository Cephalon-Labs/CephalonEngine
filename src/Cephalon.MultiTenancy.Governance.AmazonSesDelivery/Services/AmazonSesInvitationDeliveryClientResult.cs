namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

/// <summary>
/// Describes the result returned by an Amazon SES invitation delivery client.
/// </summary>
public sealed class AmazonSesInvitationDeliveryClientResult
{
    /// <summary>
    /// Creates an Amazon SES invitation delivery client result.
    /// </summary>
    /// <param name="accepted">A value indicating whether Amazon SES accepted the request.</param>
    /// <param name="statusCode">The HTTP status code reported by the AWS SDK when one is known.</param>
    /// <param name="providerMessageId">The Amazon SES message identifier when one is known.</param>
    /// <param name="reason">The provider-facing outcome reason.</param>
    /// <param name="metadata">Optional safe client metadata.</param>
    public AmazonSesInvitationDeliveryClientResult(
        bool accepted,
        int? statusCode = null,
        string? providerMessageId = null,
        string? reason = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Accepted = accepted;
        StatusCode = statusCode;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets a value indicating whether Amazon SES accepted the request.
    /// </summary>
    public bool Accepted { get; }

    /// <summary>
    /// Gets the HTTP status code reported by the AWS SDK when one is known.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the Amazon SES message identifier when one is known.
    /// </summary>
    public string? ProviderMessageId { get; }

    /// <summary>
    /// Gets the provider-facing outcome reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets optional safe client metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
