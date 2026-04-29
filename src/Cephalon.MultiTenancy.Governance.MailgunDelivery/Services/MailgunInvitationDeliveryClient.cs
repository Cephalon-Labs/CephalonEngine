using Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;

internal sealed class MailgunInvitationDeliveryClient(
    IHttpClientFactory httpClientFactory,
    MailgunInvitationDeliveryOptions options) : IMailgunInvitationDeliveryClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<MailgunInvitationDeliveryClientResult> SendAsync(
        MailgunInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.GetTimeout());

        var client = httpClientFactory.CreateClient(MailgunInvitationDeliveryServiceCollectionExtensions.HttpClientName);
        var endpoint = options.GetMessagesEndpoint();
        using var request = CreateRequest(message, endpoint);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        var statusCode = (int)response.StatusCode;
        var responseBody = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        var accepted = IsAccepted(statusCode);
        var providerMessageId = GetProviderMessageId(responseBody);
        var responseMessage = GetResponseMessage(responseBody);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mailgunStatusCode"] = statusCode.ToString(CultureInfo.InvariantCulture),
            ["mailgunEndpoint"] = endpoint.ToString()
        };

        if (!string.IsNullOrWhiteSpace(response.ReasonPhrase))
        {
            metadata["mailgunReasonPhrase"] = response.ReasonPhrase.Trim();
        }

        if (!string.IsNullOrWhiteSpace(providerMessageId))
        {
            metadata["mailgunMessageId"] = providerMessageId;
        }

        if (!string.IsNullOrWhiteSpace(responseMessage))
        {
            metadata["mailgunResponseMessage"] = responseMessage;
        }

        return new MailgunInvitationDeliveryClientResult(
            accepted,
            statusCode,
            providerMessageId,
            accepted
                ? $"Mailgun Messages API accepted the message with status code {statusCode}."
                : $"Mailgun Messages API returned status code {statusCode}.",
            metadata);
    }

    private HttpRequestMessage CreateRequest(MailgunInvitationDeliveryMessage message, Uri endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = CreateContent(message)
        };

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes("api:" + options.ApiKey!.Trim()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        return request;
    }

    private static MultipartFormDataContent CreateContent(MailgunInvitationDeliveryMessage message)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(message.From, Encoding.UTF8), "from" },
            { new StringContent(message.To, Encoding.UTF8), "to" },
            { new StringContent(message.Subject, Encoding.UTF8), "subject" },
            { new StringContent(message.TextBody, Encoding.UTF8), "text" }
        };

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            content.Add(new StringContent(message.HtmlBody, Encoding.UTF8), "html");
        }

        foreach (var tag in message.Tags)
        {
            content.Add(new StringContent(tag, Encoding.UTF8), "o:tag");
        }

        if (message.TestMode)
        {
            content.Add(new StringContent("yes", Encoding.UTF8), "o:testmode");
        }

        foreach (var variable in message.Variables)
        {
            content.Add(new StringContent(variable.Value, Encoding.UTF8), "v:" + variable.Key);
        }

        foreach (var header in message.Headers)
        {
            content.Add(new StringContent(header.Value, Encoding.UTF8), "h:" + header.Key);
        }

        return content;
    }

    private bool IsAccepted(int statusCode) => options.GetAcceptedStatusCodes().Contains(statusCode);

    private string? GetProviderMessageId(string responseBody)
    {
        return TryReadStringProperty(responseBody, options.GetProviderMessageIdJsonPropertyName());
    }

    private static string? GetResponseMessage(string responseBody)
    {
        return TryReadStringProperty(responseBody, "message");
    }

    private static string? TryReadStringProperty(string responseBody, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(responseBody) || string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String
                ? property.GetString()?.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
