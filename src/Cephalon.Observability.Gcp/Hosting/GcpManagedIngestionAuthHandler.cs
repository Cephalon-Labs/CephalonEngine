using System.Net.Http.Headers;
using Google.Apis.Auth.OAuth2;

namespace Cephalon.Observability.Gcp.Hosting;

internal sealed class GcpManagedIngestionAuthHandler : DelegatingHandler
{
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    private readonly SemaphoreSlim credentialLock = new(1, 1);
    private readonly string? quotaProjectId;

    private GoogleCredential? credential;

    public GcpManagedIngestionAuthHandler(string? quotaProjectId)
        : base(new HttpClientHandler())
    {
        this.quotaProjectId = string.IsNullOrWhiteSpace(quotaProjectId)
            ? null
            : quotaProjectId.Trim();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var resolvedCredential = await GetCredentialAsync(cancellationToken).ConfigureAwait(false);
        var tokenAccess = resolvedCredential.UnderlyingCredential as ITokenAccessWithHeaders
            ?? throw new InvalidOperationException(
                $"Google credential type '{resolvedCredential.UnderlyingCredential.GetType().FullName}' does not support access-token retrieval with headers.");
        var accessTokenWithHeaders = await tokenAccess
            .GetAccessTokenWithHeadersForRequestAsync(request.RequestUri?.AbsoluteUri, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(accessTokenWithHeaders.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenWithHeaders.AccessToken);
        }

        foreach (var header in accessTokenWithHeaders.Headers)
        {
            request.Headers.Remove(header.Key);
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (!string.IsNullOrWhiteSpace(quotaProjectId))
        {
            request.Headers.Remove("x-goog-user-project");
            request.Headers.TryAddWithoutValidation("x-goog-user-project", quotaProjectId);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GoogleCredential> GetCredentialAsync(CancellationToken cancellationToken)
    {
        if (credential is not null)
        {
            return credential;
        }

        await credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (credential is null)
            {
                var applicationDefaultCredential = await GoogleCredential
                    .GetApplicationDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                credential = applicationDefaultCredential.IsCreateScopedRequired
                    ? applicationDefaultCredential.CreateScoped(CloudPlatformScope)
                    : applicationDefaultCredential;
            }

            return credential;
        }
        finally
        {
            credentialLock.Release();
        }
    }
}
