using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Defaults;
using Cephalon.Diagnostics.Redaction.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Diagnostics.Redaction;

public sealed class RedactionServiceCollectionExtensionsTests
{
    private static readonly RedactionContext SampleContext = new(
        ActivitySourceName: "Cephalon.AspNetCore",
        MeterName: null,
        AttributeKey: "http.request.header.authorization",
        LoggerCategory: null);

    [Fact]
    public void AddRedactionPipeline_RegistersPipelineComposedFromRegisteredFilters()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRedactionFilter>(
            new KeyMatchRedactionFilter(["http.request.header.authorization"]));
        services.AddRedactionPipeline();

        using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<RedactionPipeline>();

        var result = pipeline.Filter(SampleContext, "Bearer abc123");

        Assert.Equal(KeyMatchRedactionFilter.DefaultReplacement, result);
        Assert.Equal(1, pipeline.Count);
    }

    [Fact]
    public void AddRedactionPipeline_ComposesFiltersInDIRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRedactionFilter>(new ConstantFilter("first"));
        services.AddSingleton<IRedactionFilter>(new ConstantFilter("second"));
        services.AddRedactionPipeline();

        using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<RedactionPipeline>();

        var result = pipeline.Filter(SampleContext, "input");

        Assert.Equal("second", result);
        Assert.Equal(2, pipeline.Count);
    }

    [Fact]
    public void AddRedactionPipeline_NoFiltersRegistered_PipelineIsEmptyPassthrough()
    {
        var services = new ServiceCollection();
        services.AddRedactionPipeline();

        using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<RedactionPipeline>();

        var result = pipeline.Filter(SampleContext, "passthrough");

        Assert.Equal("passthrough", result);
        Assert.Equal(0, pipeline.Count);
    }

    [Fact]
    public void AddRedactionPipeline_PipelineIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddRedactionPipeline();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<RedactionPipeline>();
        var second = provider.GetRequiredService<RedactionPipeline>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddRedactionPipeline_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRedactionFilter>(new ConstantFilter("only"));
        services.AddRedactionPipeline();
        services.AddRedactionPipeline();
        services.AddRedactionPipeline();

        using var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<RedactionPipeline>();

        Assert.Equal(1, pipeline.Count);
    }

    [Fact]
    public void AddRedactionPipeline_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddRedactionPipeline();

        Assert.Same(services, returned);
    }

    [Fact]
    public void AddRedactionPipeline_ThrowsOnNullServiceCollection()
    {
        Assert.Throws<ArgumentNullException>(
            () => RedactionServiceCollectionExtensions.AddRedactionPipeline(services: null!));
    }

    private sealed class ConstantFilter : IRedactionFilter
    {
        private readonly object? replacement;

        public ConstantFilter(object? replacement) => this.replacement = replacement;

        public object? Filter(RedactionContext context, object? value) => replacement;
    }
}
