namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

/// <summary>
/// Represents the result of confirming an Amazon SNS subscription for Amazon SES callbacks.
/// </summary>
public sealed class AmazonSesSnsSubscriptionConfirmationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSesSnsSubscriptionConfirmationResult" /> class.
    /// </summary>
    /// <param name="succeeded">A value indicating whether the subscription confirmation succeeded.</param>
    /// <param name="outcome">The stable confirmation outcome.</param>
    /// <param name="reason">A human-readable reason for the confirmation result.</param>
    /// <param name="statusCode">The provider HTTP status code, when one was observed.</param>
    /// <param name="metadata">Safe provider metadata for operator reporting.</param>
    public AmazonSesSnsSubscriptionConfirmationResult(
        bool succeeded,
        string outcome,
        string reason,
        int? statusCode = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        Succeeded = succeeded;
        Outcome = outcome.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? Outcome : reason.Trim();
        StatusCode = statusCode;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a value indicating whether the subscription confirmation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the stable confirmation outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a human-readable reason for the confirmation result.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the provider HTTP status code, when one was observed.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets safe provider metadata for operator reporting.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates a successful SNS subscription-confirmation result.
    /// </summary>
    /// <param name="statusCode">The observed provider status code.</param>
    /// <param name="metadata">Safe provider metadata for operator reporting.</param>
    /// <returns>A successful confirmation result.</returns>
    public static AmazonSesSnsSubscriptionConfirmationResult Confirmed(
        int? statusCode = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            succeeded: true,
            outcome: "confirmed",
            reason: "Amazon SNS subscription confirmation succeeded.",
            statusCode,
            metadata);
}
