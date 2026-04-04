using Cephalon.Abstractions.Tenancy;
using Cephalon.MultiTenancy.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Services;

internal sealed class ConfiguredTenantResolver(
    MultiTenancyRuntimeOptions options,
    ILogger<ConfiguredTenantResolver> logger) : ITenantResolver
{
    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var resolved = ResolveConfiguredTenant(request);
        AmbientTenantContextAccessor.SetCurrent(resolved.Tenant);

        if (resolved.Tenant is not null)
        {
            if (string.Equals(resolved.Source, "default-tenant", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resolved.Source, "single-tenant-fallback", StringComparison.OrdinalIgnoreCase))
            {
                MultiTenancyLoggerMessages.TenantResolutionDefaulted(
                    logger,
                    resolved.Tenant.TenantId,
                    resolved.Source ?? "default-tenant",
                    null);
            }
            else
            {
                MultiTenancyLoggerMessages.TenantResolved(
                    logger,
                    resolved.Tenant.TenantId,
                    resolved.Source ?? "configured",
                    null);
            }
        }
        else
        {
            MultiTenancyLoggerMessages.TenantResolutionMissed(
                logger,
                resolved.Reason ?? "No configured tenant matched the supplied request hints.",
                null);
        }

        return ValueTask.FromResult(resolved);
    }

    private TenantResolutionResult ResolveConfiguredTenant(TenantResolutionRequest request)
    {
        if (options.Tenants.Count == 0)
        {
            return new TenantResolutionResult(
                tenant: null,
                source: "configured-directory",
                reason: "No configured tenants are available.");
        }

        var requestedTenantId = Normalize(request.RequestedTenantId);
        if (requestedTenantId is not null)
        {
            var tenant = options.Tenants.FirstOrDefault(candidate =>
                string.Equals(candidate.TenantId, requestedTenantId, StringComparison.OrdinalIgnoreCase));
            if (tenant is not null)
            {
                return new TenantResolutionResult(tenant, source: "requested-tenant-id");
            }
        }

        var requestedTenantKey = Normalize(request.RequestedTenantKey);
        if (requestedTenantKey is not null)
        {
            var tenant = options.Tenants.FirstOrDefault(candidate =>
                string.Equals(candidate.TenantKey, requestedTenantKey, StringComparison.OrdinalIgnoreCase));
            if (tenant is not null)
            {
                return new TenantResolutionResult(tenant, source: "requested-tenant-key");
            }
        }

        var hostName = Normalize(request.HostName);
        if (hostName is not null)
        {
            var tenant = options.Tenants.FirstOrDefault(candidate =>
                candidate.Domains.Any(domain => string.Equals(domain, hostName, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(candidate.TenantKey, GetLeftmostHostLabel(hostName), StringComparison.OrdinalIgnoreCase));
            if (tenant is not null)
            {
                return new TenantResolutionResult(tenant, source: "host-name");
            }
        }

        if (HasExplicitResolutionHint(requestedTenantId, requestedTenantKey, hostName))
        {
            return new TenantResolutionResult(
                tenant: null,
                source: "configured-directory",
                reason: "No configured tenant matched the supplied request hints.");
        }

        var defaultTenantId = Normalize(options.DefaultTenantId);
        if (defaultTenantId is not null)
        {
            var tenant = options.Tenants.FirstOrDefault(candidate =>
                string.Equals(candidate.TenantId, defaultTenantId, StringComparison.OrdinalIgnoreCase));
            if (tenant is not null)
            {
                return new TenantResolutionResult(tenant, source: "default-tenant");
            }
        }

        if (options.Tenants.Count == 1)
        {
            return new TenantResolutionResult(options.Tenants[0], source: "single-tenant-fallback");
        }

        return new TenantResolutionResult(
            tenant: null,
            source: "configured-directory",
            reason: "No configured tenant matched the supplied request hints.");
    }

    private static string? GetLeftmostHostLabel(string hostName)
    {
        var normalizedHostName = Normalize(hostName);
        if (normalizedHostName is null)
        {
            return null;
        }

        var separatorIndex = normalizedHostName.IndexOf('.');
        return separatorIndex < 0
            ? normalizedHostName
            : normalizedHostName[..separatorIndex];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool HasExplicitResolutionHint(
        string? requestedTenantId,
        string? requestedTenantKey,
        string? hostName)
    {
        return requestedTenantId is not null ||
            requestedTenantKey is not null ||
            hostName is not null;
    }
}

internal static class MultiTenancyLoggerMessages
{
    private static readonly Action<ILogger, string, string, Exception?> TenantResolvedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(MultiTenancyDiagnosticsConventions.TenantResolved.Id, MultiTenancyDiagnosticsConventions.TenantResolved.Name),
            MultiTenancyDiagnosticsConventions.TenantResolved.MessageTemplate);

    private static readonly Action<ILogger, string, string, Exception?> TenantResolutionDefaultedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(MultiTenancyDiagnosticsConventions.TenantResolutionDefaulted.Id, MultiTenancyDiagnosticsConventions.TenantResolutionDefaulted.Name),
            MultiTenancyDiagnosticsConventions.TenantResolutionDefaulted.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> TenantResolutionMissedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(MultiTenancyDiagnosticsConventions.TenantResolutionMissed.Id, MultiTenancyDiagnosticsConventions.TenantResolutionMissed.Name),
            MultiTenancyDiagnosticsConventions.TenantResolutionMissed.MessageTemplate);

    public static void TenantResolved(ILogger logger, string tenantId, string source, Exception? exception)
    {
        TenantResolvedMessage(logger, tenantId, source, exception);
    }

    public static void TenantResolutionDefaulted(ILogger logger, string tenantId, string source, Exception? exception)
    {
        TenantResolutionDefaultedMessage(logger, tenantId, source, exception);
    }

    public static void TenantResolutionMissed(ILogger logger, string reason, Exception? exception)
    {
        TenantResolutionMissedMessage(logger, reason, exception);
    }
}

internal sealed class DisabledTenantResolver(
    ILogger<DisabledTenantResolver> logger) : ITenantResolver
{
    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        AmbientTenantContextAccessor.SetCurrent(null);

        const string reason = "The configuration-driven tenant resolver is disabled.";
        MultiTenancyLoggerMessages.TenantResolutionMissed(logger, reason, null);

        return ValueTask.FromResult(new TenantResolutionResult(
            tenant: null,
            source: "disabled",
            reason: reason));
    }
}
