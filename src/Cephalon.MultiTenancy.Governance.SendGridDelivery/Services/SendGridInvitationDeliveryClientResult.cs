namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

/// <summary>
/// Describes the result returned by a SendGrid invitation delivery client.
/// </summary>
public sealed class SendGridInvitationDeliveryClientResult
{
    /// <summary>
    /// Creates a SendGrid invitation delivery client result.
    /// </summary>
    /// <param name="accepted">A value indicating whether the SendGrid API accepted the request.</param>
    /// <param name="statusCode">The HTTP status code returned by SendGrid when one is known.</param>
    /// <param name="providerMessageId">The SendGrid message identifier when one is known.</param>
    /// <param name="reason">The provider-facing outcome reason.</param>
    /// <param name="metadata">Optional safe client metadata.</param>
    public SendGridInvitationDeliveryClientResult(
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
    /// Gets a value indicating whether the SendGrid API accepted the request.
    /// </summary>
    public bool Accepted { get; }

    /// <summary>
    /// Gets the HTTP status code returned by SendGrid when one is known.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the SendGrid message identifier when one is known.
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
