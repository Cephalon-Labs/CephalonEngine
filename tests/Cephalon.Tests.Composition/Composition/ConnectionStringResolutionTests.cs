using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Tests.Composition;

public sealed class ConnectionStringResolutionTests
{
    [Fact]
    public void Resolve_PrefersInlineConnectionString_WhenProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDB"] = "mongodb://configured:27017"
            })
            .Build();

        var resolved = ConnectionStringResolution.Resolve(
            configuration,
            connectionString: "mongodb://inline:27017",
            connectionStringName: null,
            defaultConnectionString: "mongodb://localhost:27017",
            sectionPath: "Engine:Data:MongoDB",
            providerDisplayName: "MongoDB");

        Assert.Equal("mongodb://inline:27017", resolved);
    }

    [Fact]
    public void Resolve_UsesNamedConnectionString_WhenInlineValueIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDB"] = "mongodb://configured:27017"
            })
            .Build();

        var resolved = ConnectionStringResolution.Resolve(
            configuration,
            connectionString: null,
            connectionStringName: "MongoDB",
            defaultConnectionString: "mongodb://localhost:27017",
            sectionPath: "Engine:Data:MongoDB",
            providerDisplayName: "MongoDB");

        Assert.Equal("mongodb://configured:27017", resolved);
    }

    [Fact]
    public void Resolve_Throws_WhenBothInlineAndNamedValuesAreProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDB"] = "mongodb://configured:27017"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectionStringResolution.Resolve(
                configuration,
                connectionString: "mongodb://inline:27017",
                connectionStringName: "MongoDB",
                defaultConnectionString: "mongodb://localhost:27017",
                sectionPath: "Engine:Data:MongoDB",
                providerDisplayName: "MongoDB"));

        Assert.Contains("either ConnectionStringName or ConnectionString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_FallsBackToDefault_WhenNoValuesAreConfigured()
    {
        var resolved = ConnectionStringResolution.Resolve(
            configuration: null,
            connectionString: null,
            connectionStringName: null,
            defaultConnectionString: "mongodb://localhost:27017",
            sectionPath: "Engine:Data:MongoDB",
            providerDisplayName: "MongoDB");

        Assert.Equal("mongodb://localhost:27017", resolved);
    }
}
