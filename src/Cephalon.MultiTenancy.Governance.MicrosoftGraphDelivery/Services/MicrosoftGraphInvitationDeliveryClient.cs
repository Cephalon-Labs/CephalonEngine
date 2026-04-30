using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

internal sealed class MicrosoftGraphInvitationDeliveryClient(
    IHttpClientFactory httpClientFactory,
    MicrosoftGraphInvitationDeliveryOptions options,
    IMicrosoftGraphInvitationDeliveryAccessTokenProvider accessTokenProvider) : IMicrosoftGraphInvitationDeliveryClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<MicrosoftGraphInvitationDeliveryClientResult> SendAsync(
        MicrosoftGraphInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new MicrosoftGraphInvitationDeliveryClientResult(
                accepted: false,
                reason: "Microsoft Graph invitation delivery requires a bearer token from the configured access-token provider.",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["microsoftGraphTokenProvider"] = accessTokenProvider.GetType().Name,
                    ["microsoftGraphTokenAvailable"] = "false"
                });
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.GetTimeout());

        var client = httpClientFactory.CreateClient(MicrosoftGraphInvitationDeliveryServiceCollectionExtensions.HttpClientName);
        var endpoint = options.GetSendMailEndpoint();
        using var request = CreateRequest(message, endpoint, accessToken);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        var statusCode = (int)response.StatusCode;
        var accepted = IsAccepted(statusCode);
        var responseBody = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        var requestId = GetHeader(response, "request-id") ?? GetHeader(response, "x-ms-request-id");
        var clientRequestId = GetHeader(response, "client-request-id");
        var errorCode = GetGraphErrorProperty(responseBody, "code");
        var errorMessage = GetGraphErrorProperty(responseBody, "message");
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["microsoftGraphStatusCode"] = statusCode.ToString(CultureInfo.InvariantCulture),
            ["microsoftGraphEndpoint"] = endpoint.ToString(),
            ["microsoftGraphTokenProvider"] = accessTokenProvider.GetType().Name,
            ["microsoftGraphTokenAvailable"] = "true"
        };

        if (!string.IsNullOrWhiteSpace(response.ReasonPhrase))
        {
            metadata["microsoftGraphReasonPhrase"] = response.ReasonPhrase.Trim();
        }

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            metadata["microsoftGraphRequestId"] = requestId;
        }

        if (!string.IsNullOrWhiteSpace(clientRequestId))
        {
            metadata["microsoftGraphClientRequestId"] = clientRequestId;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            metadata["microsoftGraphErrorCode"] = errorCode;
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            metadata["microsoftGraphErrorMessage"] = errorMessage;
        }

        return new MicrosoftGraphInvitationDeliveryClientResult(
            accepted,
            statusCode,
            providerMessageId: null,
            accepted
                ? $"Microsoft Graph sendMail accepted the message with status code {statusCode}."
                : $"Microsoft Graph sendMail returned status code {statusCode}.",
            metadata);
    }

    private static HttpRequestMessage CreateRequest(MicrosoftGraphInvitationDeliveryMessage message, Uri endpoint, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(CreateRequestBody(message), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("client-request-id", message.MessageId);
        request.Headers.TryAddWithoutValidation("return-client-request-id", "true");
        return request;
    }

    private static string CreateRequestBody(MicrosoftGraphInvitationDeliveryMessage message)
    {
        var graphMessage = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["subject"] = message.Subject,
            ["body"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["contentType"] = message.HasHtmlBody ? "HTML" : "Text",
                ["content"] = message.HasHtmlBody ? message.HtmlBody! : message.TextBody
            },
            ["toRecipients"] = new[]
            {
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["emailAddress"] = CreateEmailAddress(message.ToEmail, message.ToName)
                }
            }
        };

        if (message.Categories.Count > 0)
        {
            graphMessage["categories"] = message.Categories;
        }

        if (message.Headers.Count > 0)
        {
            graphMessage["internetMessageHeaders"] = message.Headers
                .Select(static pair => new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = pair.Key,
                    ["value"] = pair.Value
                })
                .ToArray();
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["message"] = graphMessage,
            ["saveToSentItems"] = message.SaveToSentItems
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static Dictionary<string, string> CreateEmailAddress(string email, string? name)
    {
        var value = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["address"] = email
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            value["name"] = name;
        }

        return value;
    }

    private bool IsAccepted(int statusCode) => options.GetAcceptedStatusCodes().Contains(statusCode);

    private static string? GetHeader(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            ? values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim()
            : null;
    }

    private static string? GetGraphErrorProperty(string responseBody, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(responseBody) || string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("error", out var error) ||
                error.ValueKind != JsonValueKind.Object ||
                !error.TryGetProperty(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return property.GetString()?.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
