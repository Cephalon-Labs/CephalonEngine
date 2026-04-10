using Cephalon.Abstractions.Data;
using Cephalon.Data.Elasticsearch.Configuration;
using Cephalon.Data.Elasticsearch.Registration;
using Cephalon.Data.OpenSearch.Configuration;
using Cephalon.Data.OpenSearch.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class SearchDataPackTests
{
    // === Elasticsearch ===

    [Fact]
    public void AddElasticsearchData_RegistersExpectedCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Elasticsearch")));
            engine.AddModule(new PlatformTestModule());
            engine.AddElasticsearchData("http://localhost:9200");
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.elasticsearch");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.search-store");
    }

    [Fact]
    public void AddElasticsearchData_WithOutbox_RegistersOutboxCapability()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Elasticsearch", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddElasticsearchData("http://localhost:9200", o => { o.RegisterOutbox = true; o.RegisterInbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.elasticsearch");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.elasticsearch");
    }

    [Fact]
    public void AddElasticsearchData_OutboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Elasticsearch", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddElasticsearchData("http://localhost:9200", o => { o.RegisterOutbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(catalog.Outboxes);
        Assert.Equal("elasticsearch-outbox", descriptor.Id);
        Assert.Equal(ElasticsearchDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("search-index", descriptor.Mode);
    }

    [Fact]
    public void AddElasticsearchData_InboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Elasticsearch")));
            engine.AddModule(new PlatformTestModule());
            engine.AddElasticsearchData("http://localhost:9200", o => { o.RegisterInbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IInboxCatalog>();
        var descriptor = Assert.Single(catalog.Inboxes);
        Assert.Equal("elasticsearch-inbox", descriptor.Id);
        Assert.Equal(ElasticsearchDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("message-id", descriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddElasticsearchData_UsesNamedUriFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:SearchCluster"] = "http://configured-es:9200"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Elasticsearch")));
            engine.AddModule(new PlatformTestModule());
            engine.AddElasticsearchData(options =>
            {
                options.UriName = "SearchCluster";
            });
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Elastic.Clients.Elasticsearch.ElasticsearchClient>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var capability = Assert.Single(runtime.Manifest.Capabilities, c => c.Key == "data.elasticsearch");

        Assert.NotNull(client);
        Assert.Equal("SearchCluster", capability.Metadata["uriName"]);
    }

    // === OpenSearch ===

    [Fact]
    public void AddOpenSearchData_RegistersExpectedCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "OpenSearch")));
            engine.AddModule(new PlatformTestModule());
            engine.AddOpenSearchData("http://localhost:9200");
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.opensearch");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.search-store");
    }

    [Fact]
    public void AddOpenSearchData_WithOutbox_RegistersOutboxCapability()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "OpenSearch", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddOpenSearchData("http://localhost:9200", o => { o.RegisterOutbox = true; o.RegisterInbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.opensearch");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.opensearch");
    }

    [Fact]
    public void AddOpenSearchData_OutboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "OpenSearch", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddOpenSearchData("http://localhost:9200", o => { o.RegisterOutbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(catalog.Outboxes);
        Assert.Equal("opensearch-outbox", descriptor.Id);
        Assert.Equal(OpenSearchDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("search-index", descriptor.Mode);
    }

    [Fact]
    public void AddOpenSearchData_InboxDescriptor_HasExpectedMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "OpenSearch")));
            engine.AddModule(new PlatformTestModule());
            engine.AddOpenSearchData("http://localhost:9200", o => { o.RegisterInbox = true; });
        });
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IInboxCatalog>();
        var descriptor = Assert.Single(catalog.Inboxes);
        Assert.Equal("opensearch-inbox", descriptor.Id);
        Assert.Equal(OpenSearchDataOptions.ProviderId, descriptor.Provider);
        Assert.Equal("message-id", descriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddOpenSearchData_ThrowsWhenBothUriAndUriNameAreConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:SearchCluster"] = "http://configured-os:9200"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "OpenSearch")));
            engine.AddModule(new PlatformTestModule());
            engine.AddOpenSearchData(options =>
            {
                options.Uri = "http://inline-os:9200";
                options.UriName = "SearchCluster";
            });
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<OpenSearch.Client.OpenSearchClient>());

        Assert.Contains("either UriName or Uri", exception.Message, StringComparison.Ordinal);
    }
}
