namespace Cephalon.Tests.ProviderIntegration.ExternalServices;

public sealed class ExternalProviderServiceGateTests
{
    [Fact]
    public void FromValues_DisablesExternalServicesByDefault()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>());

        Assert.False(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveRedisMode());
        Assert.Contains(ExternalProviderServiceGate.ExternalServicesVariable, ExternalProviderServiceGate.SkipReason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("on")]
    public void FromValues_EnablesExternalServicesForTruthyValues(string value)
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = value
        });

        Assert.True(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveRedisMode());
    }

    [Fact]
    public void FromValues_AllowsAliasToEnableExternalServices()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesAliasVariable] = "1"
        });

        Assert.True(gate.ExternalServicesEnabled);
    }

    [Fact]
    public void FromValues_RequiresExternalServicesBeforeTestcontainersModeCanRun()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.TestcontainersVariable] = "1"
        });

        Assert.False(gate.ExternalServicesEnabled);
        Assert.False(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Disabled, gate.ResolveRedisMode());
    }

    [Fact]
    public void FromValues_UsesTestcontainersWhenExternalServicesAndTestcontainersAreEnabled()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.TestcontainersVariable] = "1"
        });

        Assert.True(gate.ExternalServicesEnabled);
        Assert.True(gate.TestcontainersEnabled);
        Assert.Equal(ExternalProviderServiceMode.Testcontainers, gate.ResolveRedisMode());
    }

    [Fact]
    public void FromValues_KeepsProviderTestsSkippedUntilAProviderModeResolves()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1"
        });

        Assert.Null(gate.GetSkipReason(ExternalProviderServiceProvider.Any));
        Assert.Contains(
            ExternalProviderServiceGate.RedisConnectionStringVariable,
            gate.GetSkipReason(ExternalProviderServiceProvider.Redis)!,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FromValues_PrefersPreProvisionedConnectionStringOverTestcontainers()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.TestcontainersVariable] = "1",
            [ExternalProviderServiceGate.RedisConnectionStringVariable] = " localhost:6379 "
        });

        Assert.Equal("localhost:6379", gate.RedisConnectionString);
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveRedisMode());
    }

    [Fact]
    public void FromValues_UsesRedisConnectionStringAlias()
    {
        var gate = ExternalProviderServiceGate.FromValues(new Dictionary<string, string?>
        {
            [ExternalProviderServiceGate.ExternalServicesVariable] = "1",
            [ExternalProviderServiceGate.RedisConnectionStringAliasVariable] = " redis.local:6379 "
        });

        Assert.Equal("redis.local:6379", gate.RedisConnectionString);
        Assert.Equal(ExternalProviderServiceMode.PreProvisionedConnectionString, gate.ResolveRedisMode());
    }

    [ExternalProviderServiceFact]
    public void ExternalProviderServiceFact_RemainsDiscoverableWhenTheLaneIsEnabled()
    {
        Assert.True(ExternalProviderServiceGate.FromEnvironment().ExternalServicesEnabled);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Redis)]
    public void RedisExternalProviderServiceFact_RemainsDiscoverableWhenAProviderModeIsEnabled()
    {
        Assert.NotEqual(ExternalProviderServiceMode.Disabled, ExternalProviderServiceGate.FromEnvironment().ResolveRedisMode());
    }
}
