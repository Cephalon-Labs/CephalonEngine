using Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

internal sealed class SendGridInvitationDeliveryClient(
    IHttpClientFactory httpClientFactory,
    SendGridInvitationDeliveryOptions options) : ISendGridInvitationDeliveryClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<SendGridInvitationDeliveryClientResult> SendAsync(
        SendGridInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.GetTimeout());

        var client = httpClientFactory.CreateClient(SendGridInvitationDeliveryServiceCollectionExtensions.HttpClientName);
        var endpoint = options.GetMailSendEndpoint();
        using var request = CreateRequest(message, endpoint);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        var statusCode = (int)response.StatusCode;
        var accepted = IsAccepted(statusCode);
        var providerMessageId = GetProviderMessageId(response);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sendGridStatusCode"] = statusCode.ToString(CultureInfo.InvariantCulture),
            ["sendGridEndpoint"] = endpoint.ToString()
        };

        if (!string.IsNullOrWhiteSpace(response.ReasonPhrase))
        {
            metadata["sendGridReasonPhrase"] = response.ReasonPhrase.Trim();
        }

        if (!string.IsNullOrWhiteSpace(providerMessageId))
        {
            metadata["sendGridMessageId"] = providerMessageId;
        }

        return new SendGridInvitationDeliveryClientResult(
            accepted,
            statusCode,
            providerMessageId,
            accepted
                ? $"SendGrid Mail Send API accepted the message with status code {statusCode}."
                : $"SendGrid Mail Send API returned status code {statusCode}.",
            metadata);
    }

    private HttpRequestMessage CreateRequest(SendGridInvitationDeliveryMessage message, Uri endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(CreateRequestBody(message), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey!.Trim());
        return request;
    }

    private static string CreateRequestBody(SendGridInvitationDeliveryMessage message)
    {
        var personalization = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["to"] = new[] { CreateEmailObject(message.ToEmail, message.ToName) },
            ["subject"] = message.Subject
        };

        if (message.CustomArgs.Count > 0)
        {
            personalization["custom_args"] = message.CustomArgs;
        }

        var content = new List<Dictionary<string, string>>
        {
            new(StringComparer.Ordinal)
            {
                ["type"] = "text/plain",
                ["value"] = message.TextBody
            }
        };

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            content.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["type"] = "text/html",
                ["value"] = message.HtmlBody
            });
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["personalizations"] = new[] { personalization },
            ["from"] = CreateEmailObject(message.FromEmail, message.FromName),
            ["subject"] = message.Subject,
            ["content"] = content
        };

        if (message.Headers.Count > 0)
        {
            payload["headers"] = message.Headers;
        }

        if (message.Categories.Count > 0)
        {
            payload["categories"] = message.Categories;
        }

        if (message.SandboxMode)
        {
            payload["mail_settings"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["sandbox_mode"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["enable"] = true
                }
            };
        }

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static Dictionary<string, string> CreateEmailObject(string email, string? name)
    {
        var value = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["email"] = email
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            value["name"] = name;
        }

        return value;
    }

    private bool IsAccepted(int statusCode)
    {
        return options.GetAcceptedStatusCodes().Contains(statusCode) ||
            (options.EnableSandboxMode && statusCode == 200);
    }

    private string? GetProviderMessageId(HttpResponseMessage response)
    {
        var headerName = options.GetProviderMessageIdHeaderName();
        return response.Headers.TryGetValues(headerName, out var values)
            ? values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim()
            : null;
    }
}
