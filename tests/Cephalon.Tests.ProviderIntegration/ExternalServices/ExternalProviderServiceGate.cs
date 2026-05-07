namespace Cephalon.Tests.ProviderIntegration.ExternalServices;

internal sealed record ExternalProviderServiceGate(
    bool ExternalServicesEnabled,
    bool TestcontainersEnabled,
    string? RedisConnectionString)
{
    internal const string ExternalServicesVariable = "CEPHALON_PROVIDER_EXTERNAL_SERVICES";
    internal const string ExternalServicesAliasVariable = "CEPHALON_PROVIDER_INTEGRATION";
    internal const string TestcontainersVariable = "CEPHALON_PROVIDER_TESTCONTAINERS";
    internal const string RedisConnectionStringVariable = "CEPHALON_PROVIDER_REDIS_CONNECTION_STRING";
    internal const string RedisConnectionStringAliasVariable = "CEPHALON_REDIS_CONNECTION_STRING";

    internal static string SkipReason =>
        $"External provider service tests are disabled. Set {ExternalServicesVariable}=1 and either {TestcontainersVariable}=1 or a provider connection string to run this lane.";

    internal static ExternalProviderServiceGate FromEnvironment()
    {
        return FromResolver(Environment.GetEnvironmentVariable);
    }

    internal static ExternalProviderServiceGate FromValues(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return FromResolver(name => values.TryGetValue(name, out var value) ? value : null);
    }

    internal ExternalProviderServiceMode ResolveRedisMode()
    {
        return ResolveProviderMode(RedisConnectionString);
    }

    internal string? GetSkipReason(ExternalProviderServiceProvider provider)
    {
        if (!ExternalServicesEnabled)
        {
            return SkipReason;
        }

        return provider switch
        {
            ExternalProviderServiceProvider.Any => null,
            ExternalProviderServiceProvider.Redis when ResolveRedisMode() == ExternalProviderServiceMode.Disabled =>
                $"Redis external-provider tests are disabled. Set {RedisConnectionStringVariable} or set {TestcontainersVariable}=1 with {ExternalServicesVariable}=1.",
            ExternalProviderServiceProvider.Redis => null,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown external provider service provider.")
        };
    }

    private static ExternalProviderServiceGate FromResolver(Func<string, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        var externalServicesEnabled =
            IsTruthy(resolver(ExternalServicesVariable)) ||
            IsTruthy(resolver(ExternalServicesAliasVariable));
        var testcontainersEnabled = externalServicesEnabled && IsTruthy(resolver(TestcontainersVariable));
        var redisConnectionString = FirstNonEmpty(
            resolver(RedisConnectionStringVariable),
            resolver(RedisConnectionStringAliasVariable));

        return new ExternalProviderServiceGate(
            externalServicesEnabled,
            testcontainersEnabled,
            redisConnectionString);
    }

    private ExternalProviderServiceMode ResolveProviderMode(string? connectionString)
    {
        if (!ExternalServicesEnabled)
        {
            return ExternalProviderServiceMode.Disabled;
        }

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return ExternalProviderServiceMode.PreProvisionedConnectionString;
        }

        return TestcontainersEnabled
            ? ExternalProviderServiceMode.Testcontainers
            : ExternalProviderServiceMode.Disabled;
    }

    private static bool IsTruthy(string? value)
    {
        return value is not null
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
