namespace Cephalon.Tests.CdcIntegration.ExternalServices;

internal sealed record ExternalCdcServiceGate(
    bool ExternalServicesEnabled,
    bool TestcontainersEnabled,
    string? SqlServerConnectionString,
    string? PostgresConnectionString,
    string? MySqlConnectionString)
{
    internal const string ExternalServicesVariable = "CEPHALON_CDC_EXTERNAL_SERVICES";
    internal const string TestcontainersVariable = "CEPHALON_CDC_TESTCONTAINERS";
    internal const string SqlServerConnectionStringVariable = "CEPHALON_CDC_SQLSERVER_CONNECTION_STRING";
    internal const string PostgresConnectionStringVariable = "CEPHALON_CDC_POSTGRES_CONNECTION_STRING";
    internal const string MySqlConnectionStringVariable = "CEPHALON_CDC_MYSQL_CONNECTION_STRING";

    internal static string SkipReason =>
        $"External CDC service tests are disabled. Set {ExternalServicesVariable}=1 and either {TestcontainersVariable}=1 or a provider connection string to run this lane.";

    internal static ExternalCdcServiceGate FromEnvironment()
    {
        return FromResolver(Environment.GetEnvironmentVariable);
    }

    internal static ExternalCdcServiceGate FromValues(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return FromResolver(name => values.TryGetValue(name, out var value) ? value : null);
    }

    internal ExternalCdcServiceMode ResolveSqlServerMode()
    {
        return ResolveProviderMode(SqlServerConnectionString);
    }

    internal ExternalCdcServiceMode ResolvePostgresMode()
    {
        return ResolveProviderMode(PostgresConnectionString);
    }

    internal ExternalCdcServiceMode ResolveMySqlMode()
    {
        return ResolveProviderMode(MySqlConnectionString);
    }

    internal string? GetSkipReason(ExternalCdcServiceProvider provider)
    {
        if (!ExternalServicesEnabled)
        {
            return SkipReason;
        }

        return provider switch
        {
            ExternalCdcServiceProvider.Any => null,
            ExternalCdcServiceProvider.SqlServer when ResolveSqlServerMode() == ExternalCdcServiceMode.Disabled =>
                $"SQL Server CDC external-service tests are disabled. Set {SqlServerConnectionStringVariable} or set {TestcontainersVariable}=1 with {ExternalServicesVariable}=1.",
            ExternalCdcServiceProvider.Postgres when ResolvePostgresMode() == ExternalCdcServiceMode.Disabled =>
                $"PostgreSQL CDC external-service tests are disabled. Set {PostgresConnectionStringVariable} or set {TestcontainersVariable}=1 with {ExternalServicesVariable}=1.",
            ExternalCdcServiceProvider.MySql when ResolveMySqlMode() == ExternalCdcServiceMode.Disabled =>
                $"MySQL CDC external-service tests are disabled. Set {MySqlConnectionStringVariable} or set {TestcontainersVariable}=1 with {ExternalServicesVariable}=1.",
            ExternalCdcServiceProvider.SqlServer or ExternalCdcServiceProvider.Postgres or ExternalCdcServiceProvider.MySql => null,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown external CDC service provider.")
        };
    }

    private static ExternalCdcServiceGate FromResolver(Func<string, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        var externalServicesEnabled = IsTruthy(resolver(ExternalServicesVariable));
        var testcontainersEnabled = externalServicesEnabled && IsTruthy(resolver(TestcontainersVariable));

        return new ExternalCdcServiceGate(
            externalServicesEnabled,
            testcontainersEnabled,
            NormalizeConnectionString(resolver(SqlServerConnectionStringVariable)),
            NormalizeConnectionString(resolver(PostgresConnectionStringVariable)),
            NormalizeConnectionString(resolver(MySqlConnectionStringVariable)));
    }

    private ExternalCdcServiceMode ResolveProviderMode(string? connectionString)
    {
        if (!ExternalServicesEnabled)
        {
            return ExternalCdcServiceMode.Disabled;
        }

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return ExternalCdcServiceMode.PreProvisionedConnectionString;
        }

        return TestcontainersEnabled
            ? ExternalCdcServiceMode.Testcontainers
            : ExternalCdcServiceMode.Disabled;
    }

    private static bool IsTruthy(string? value)
    {
        return value is not null
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeConnectionString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
