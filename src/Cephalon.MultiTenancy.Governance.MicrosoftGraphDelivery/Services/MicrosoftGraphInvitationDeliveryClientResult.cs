namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

/// <summary>
/// Describes the result returned by a Microsoft Graph invitation delivery client.
/// </summary>
public sealed class MicrosoftGraphInvitationDeliveryClientResult
{
    /// <summary>
    /// Creates a Microsoft Graph invitation delivery client result.
    /// </summary>
    /// <param name="accepted">A value indicating whether Microsoft Graph accepted the request.</param>
    /// <param name="statusCode">The HTTP status code returned by Microsoft Graph when one is known.</param>
    /// <param name="providerMessageId">The provider message identifier when one is known.</param>
    /// <param name="reason">The provider-facing outcome reason.</param>
    /// <param name="metadata">Optional safe client metadata.</param>
    public MicrosoftGraphInvitationDeliveryClientResult(
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
    /// Gets a value indicating whether Microsoft Graph accepted the request.
    /// </summary>
    public bool Accepted { get; }

    /// <summary>
    /// Gets the HTTP status code returned by Microsoft Graph when one is known.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the provider message identifier when one is known.
    /// </summary>
    /// <remarks>
    /// Microsoft Graph <c>sendMail</c> normally returns <c>202 Accepted</c> without a message id. Implementations should
    /// leave this value empty unless they have a real provider message identifier rather than a request-id header.
    /// </remarks>
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
