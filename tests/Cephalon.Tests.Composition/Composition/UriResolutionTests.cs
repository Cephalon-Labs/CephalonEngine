using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Tests.Composition;

public sealed class UriResolutionTests
{
    [Fact]
    public void Resolve_PrefersInlineUri_WhenProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:Search"] = "http://configured:9200"
            })
            .Build();

        var resolved = UriResolution.Resolve(
            configuration,
            uri: "http://inline:9200",
            uriName: null,
            defaultUri: "http://localhost:9200",
            sectionPath: "Engine:Data:Elasticsearch",
            providerDisplayName: "Elasticsearch");

        Assert.Equal("http://inline:9200", resolved);
    }

    [Fact]
    public void Resolve_UsesNamedUri_WhenInlineValueIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:Search"] = "http://configured:9200"
            })
            .Build();

        var resolved = UriResolution.Resolve(
            configuration,
            uri: null,
            uriName: "Search",
            defaultUri: "http://localhost:9200",
            sectionPath: "Engine:Data:Elasticsearch",
            providerDisplayName: "Elasticsearch");

        Assert.Equal("http://configured:9200", resolved);
    }

    [Fact]
    public void Resolve_Throws_WhenBothInlineAndNamedValuesAreProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:Search"] = "http://configured:9200"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            UriResolution.Resolve(
                configuration,
                uri: "http://inline:9200",
                uriName: "Search",
                defaultUri: "http://localhost:9200",
                sectionPath: "Engine:Data:Elasticsearch",
                providerDisplayName: "Elasticsearch"));

        Assert.Contains("either UriName or Uri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_FallsBackToDefault_WhenNoValuesAreConfigured()
    {
        var resolved = UriResolution.Resolve(
            configuration: null,
            uri: null,
            uriName: null,
            defaultUri: "http://localhost:9200",
            sectionPath: "Engine:Data:Elasticsearch",
            providerDisplayName: "Elasticsearch");

        Assert.Equal("http://localhost:9200", resolved);
    }
}
