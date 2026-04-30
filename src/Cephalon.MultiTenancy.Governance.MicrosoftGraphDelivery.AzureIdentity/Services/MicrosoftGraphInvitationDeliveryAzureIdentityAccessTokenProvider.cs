using Azure.Core;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Services;

internal sealed class MicrosoftGraphInvitationDeliveryAzureIdentityAccessTokenProvider(
    MicrosoftGraphInvitationDeliveryAzureIdentityCredentialSource credentialSource,
    MicrosoftGraphInvitationDeliveryAzureIdentityOptions options,
    ILogger<MicrosoftGraphInvitationDeliveryAzureIdentityAccessTokenProvider> logger) : IMicrosoftGraphInvitationDeliveryAccessTokenProvider
{
    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scopes = options.GetScopes();
        var credentialType = credentialSource.Credential.GetType().Name;

        try
        {
            var token = await credentialSource.Credential
                .GetTokenAsync(new TokenRequestContext(scopes), cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token.Token))
            {
                MicrosoftGraphInvitationDeliveryAzureIdentityLogs.Failed(
                    logger,
                    scopes.Length,
                    credentialType,
                    "empty-token",
                    null);

                return null;
            }

            MicrosoftGraphInvitationDeliveryAzureIdentityLogs.Acquired(
                logger,
                scopes.Length,
                credentialType,
                token.ExpiresOn);

            return token.Token;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            MicrosoftGraphInvitationDeliveryAzureIdentityLogs.Failed(
                logger,
                scopes.Length,
                credentialType,
                exception.GetType().Name,
                exception);

            return null;
        }
    }
}

internal sealed class MicrosoftGraphInvitationDeliveryAzureIdentityCredentialSource(TokenCredential credential)
{
    public TokenCredential Credential { get; } = credential ?? throw new ArgumentNullException(nameof(credential));
}

internal static class MicrosoftGraphInvitationDeliveryAzureIdentityLogs
{
    private static readonly Action<ILogger, int, string, DateTimeOffset, Exception?> TokenAcquiredMessage =
        LoggerMessage.Define<int, string, DateTimeOffset>(
            LogLevel.Debug,
            new EventId(
                MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventions.MicrosoftGraphAzureIdentityTokenAcquired.Id,
                MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventions.MicrosoftGraphAzureIdentityTokenAcquired.Name),
            "Microsoft Graph Azure Identity token provider acquired a token for {ScopeCount} scopes using credential '{CredentialType}' that expires at {ExpiresOnUtc}.");

    private static readonly Action<ILogger, int, string, string, Exception?> TokenFailedMessage =
        LoggerMessage.Define<int, string, string>(
            LogLevel.Warning,
            new EventId(
                MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventions.MicrosoftGraphAzureIdentityTokenFailed.Id,
                MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventions.MicrosoftGraphAzureIdentityTokenFailed.Name),
            "Microsoft Graph Azure Identity token provider failed to acquire a token for {ScopeCount} scopes using credential '{CredentialType}'. Reason: {Reason}.");

    public static void Acquired(ILogger logger, int scopeCount, string credentialType, DateTimeOffset expiresOnUtc) =>
        TokenAcquiredMessage(logger, scopeCount, credentialType, expiresOnUtc, null);

    public static void Failed(ILogger logger, int scopeCount, string credentialType, string reason, Exception? exception) =>
        TokenFailedMessage(logger, scopeCount, credentialType, reason, exception);
}
