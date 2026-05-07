namespace Cephalon.Tests.CdcIntegration.ExternalServices;

public sealed class ExternalCdcServiceGateTests
{
    [Fact]
    public void FromValues_DisablesExternalServicesByDefault()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>());

        Assert.False(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolvePostgresMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveMySqlMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveOracleMode());
        Assert.Contains(ExternalCdcServiceGate.ExternalServicesVariable, ExternalCdcServiceGate.SkipReason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("on")]
    public void FromValues_EnablesExternalServicesForTruthyValues(string value)
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.ExternalServicesVariable] = value
        });

        Assert.True(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolvePostgresMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveMySqlMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveOracleMode());
    }

    [Fact]
    public void FromValues_RequiresExternalServicesBeforeTestcontainersModeCanRun()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.TestcontainersVariable] = "1"
        });

        Assert.False(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolvePostgresMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveMySqlMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveOracleMode());
    }

    [Fact]
    public void FromValues_UsesTestcontainersWhenExternalServicesAndTestcontainersAreEnabled()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.ExternalServicesVariable] = "1",
            [ExternalCdcServiceGate.TestcontainersVariable] = "1"
        });

        Assert.True(gate.ExternalServicesEnabled);
        Assert.True(gate.TestcontainersEnabled);
        Assert.Equal(ExternalCdcServiceMode.Testcontainers, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.Testcontainers, gate.ResolvePostgresMode());
        Assert.Equal(ExternalCdcServiceMode.Testcontainers, gate.ResolveMySqlMode());
        Assert.Equal(ExternalCdcServiceMode.Testcontainers, gate.ResolveOracleMode());
    }

    [Fact]
    public void FromValues_KeepsProviderTestsSkippedUntilAProviderModeResolves()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.ExternalServicesVariable] = "1"
        });

        Assert.Null(gate.GetSkipReason(ExternalCdcServiceProvider.Any));
        Assert.Contains(
            ExternalCdcServiceGate.SqlServerConnectionStringVariable,
            gate.GetSkipReason(ExternalCdcServiceProvider.SqlServer)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalCdcServiceGate.PostgresConnectionStringVariable,
            gate.GetSkipReason(ExternalCdcServiceProvider.Postgres)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalCdcServiceGate.MySqlConnectionStringVariable,
            gate.GetSkipReason(ExternalCdcServiceProvider.MySql)!,
            StringComparison.Ordinal);
        Assert.Contains(
            ExternalCdcServiceGate.OracleConnectionStringVariable,
            gate.GetSkipReason(ExternalCdcServiceProvider.Oracle)!,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FromValues_AllowsOnePreProvisionedProviderWithoutEnablingTheOtherProvider()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.ExternalServicesVariable] = "1",
            [ExternalCdcServiceGate.SqlServerConnectionStringVariable] = " Server=.;Database=cephalon; "
        });

        Assert.Null(gate.GetSkipReason(ExternalCdcServiceProvider.SqlServer));
        Assert.NotNull(gate.GetSkipReason(ExternalCdcServiceProvider.Postgres));
        Assert.NotNull(gate.GetSkipReason(ExternalCdcServiceProvider.MySql));
        Assert.NotNull(gate.GetSkipReason(ExternalCdcServiceProvider.Oracle));
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolvePostgresMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveMySqlMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolveOracleMode());
    }

    [Fact]
    public void FromValues_PrefersPreProvisionedConnectionStringOverTestcontainers()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.ExternalServicesVariable] = "1",
            [ExternalCdcServiceGate.TestcontainersVariable] = "1",
            [ExternalCdcServiceGate.SqlServerConnectionStringVariable] = " Server=.;Database=cephalon; ",
            [ExternalCdcServiceGate.PostgresConnectionStringVariable] = " Host=localhost;Database=cephalon; ",
            [ExternalCdcServiceGate.MySqlConnectionStringVariable] = " Server=localhost;Database=cephalon;User ID=root; ",
            [ExternalCdcServiceGate.OracleConnectionStringVariable] = " User Id=cephalon;Password=secret;Data Source=localhost/XEPDB1; "
        });

        Assert.Equal("Server=.;Database=cephalon;", gate.SqlServerConnectionString);
        Assert.Equal("Host=localhost;Database=cephalon;", gate.PostgresConnectionString);
        Assert.Equal("Server=localhost;Database=cephalon;User ID=root;", gate.MySqlConnectionString);
        Assert.Equal("User Id=cephalon;Password=secret;Data Source=localhost/XEPDB1;", gate.OracleConnectionString);
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolvePostgresMode());
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolveMySqlMode());
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolveOracleMode());
    }

    [ExternalCdcServiceFact]
    public void ExternalCdcServiceFact_RemainsDiscoverableWhenTheLaneIsEnabled()
    {
        Assert.True(ExternalCdcServiceGate.FromEnvironment().ExternalServicesEnabled);
    }

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.SqlServer)]
    public void SqlServerExternalCdcServiceFact_RemainsDiscoverableWhenAProviderModeIsEnabled()
    {
        Assert.NotEqual(ExternalCdcServiceMode.Disabled, ExternalCdcServiceGate.FromEnvironment().ResolveSqlServerMode());
    }

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.MySql)]
    public void MySqlExternalCdcServiceFact_RemainsDiscoverableWhenAProviderModeIsEnabled()
    {
        Assert.NotEqual(ExternalCdcServiceMode.Disabled, ExternalCdcServiceGate.FromEnvironment().ResolveMySqlMode());
    }

    [ExternalCdcServiceFact(ExternalCdcServiceProvider.Oracle)]
    public void OracleExternalCdcServiceFact_RemainsDiscoverableWhenAProviderModeIsEnabled()
    {
        Assert.NotEqual(ExternalCdcServiceMode.Disabled, ExternalCdcServiceGate.FromEnvironment().ResolveOracleMode());
    }
}
