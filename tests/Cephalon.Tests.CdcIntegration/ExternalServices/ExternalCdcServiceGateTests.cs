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
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.Disabled, gate.ResolvePostgresMode());
    }

    [Fact]
    public void FromValues_PrefersPreProvisionedConnectionStringOverTestcontainers()
    {
        var gate = ExternalCdcServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalCdcServiceGate.ExternalServicesVariable] = "1",
            [ExternalCdcServiceGate.TestcontainersVariable] = "1",
            [ExternalCdcServiceGate.SqlServerConnectionStringVariable] = " Server=.;Database=cephalon; ",
            [ExternalCdcServiceGate.PostgresConnectionStringVariable] = " Host=localhost;Database=cephalon; "
        });

        Assert.Equal("Server=.;Database=cephalon;", gate.SqlServerConnectionString);
        Assert.Equal("Host=localhost;Database=cephalon;", gate.PostgresConnectionString);
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolveSqlServerMode());
        Assert.Equal(ExternalCdcServiceMode.PreProvisionedConnectionString, gate.ResolvePostgresMode());
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
}
