using Cephalon.Diagnostics.Redaction;

namespace Cephalon.Tests.Diagnostics.Redaction;

public sealed class RedactionPipelineTests
{
    private static readonly RedactionContext SampleContext = new(
        ActivitySourceName: "Cephalon.AspNetCore",
        MeterName: null,
        AttributeKey: "http.request.url.full",
        LoggerCategory: null);

    [Fact]
    public void Filter_EmptyPipeline_ReturnsValueUnchanged()
    {
        var pipeline = new RedactionPipeline([]);

        var result = pipeline.Filter(SampleContext, "no filters");

        Assert.Equal("no filters", result);
        Assert.Equal(0, pipeline.Count);
    }

    [Fact]
    public void Filter_AppliesFiltersInRegistrationOrder()
    {
        var pipeline = new RedactionPipeline(
        [
            new ReplaceFilter("a", "b"),
            new ReplaceFilter("b", "c"),
        ]);

        var result = pipeline.Filter(SampleContext, "a");

        Assert.Equal("c", result);
    }

    [Fact]
    public void Filter_LaterFiltersSeePriorRedactedValue()
    {
        var observed = new List<object?>();
        var pipeline = new RedactionPipeline(
        [
            new ReplaceFilter("first", "second"),
            new ObservingFilter(observed),
        ]);

        pipeline.Filter(SampleContext, "first");

        Assert.Equal(["second"], observed);
    }

    [Fact]
    public void Filter_NullValueFlowsThroughPipeline()
    {
        var pipeline = new RedactionPipeline([new IdentityFilter()]);

        var result = pipeline.Filter(SampleContext, value: null);

        Assert.Null(result);
    }

    [Fact]
    public void Constructor_MaterialisesFiltersAtConstruction_LaterMutationsIgnored()
    {
        var source = new List<IRedactionFilter> { new ReplaceFilter("a", "b") };
        var pipeline = new RedactionPipeline(source);

        source.Add(new ReplaceFilter("b", "c"));

        var result = pipeline.Filter(SampleContext, "a");

        Assert.Equal("b", result);
        Assert.Equal(1, pipeline.Count);
    }

    [Fact]
    public void Constructor_ThrowsOnNullFilterCollection()
    {
        Assert.Throws<ArgumentNullException>(() => new RedactionPipeline(filters: null!));
    }

    [Fact]
    public void Constructor_ThrowsOnNullFilterInside()
    {
        Assert.Throws<ArgumentException>(
            () => new RedactionPipeline([new IdentityFilter(), null!]));
    }

    [Fact]
    public void Pipeline_IsItselfAnIRedactionFilter()
    {
        var inner = new RedactionPipeline([new ReplaceFilter("x", "y")]);
        var outer = new RedactionPipeline([inner, new ReplaceFilter("y", "z")]);

        var result = outer.Filter(SampleContext, "x");

        Assert.Equal("z", result);
    }

    private sealed class ReplaceFilter : IRedactionFilter
    {
        private readonly object? match;
        private readonly object? replacement;

        public ReplaceFilter(object? match, object? replacement)
        {
            this.match = match;
            this.replacement = replacement;
        }

        public object? Filter(RedactionContext context, object? value)
            => Equals(value, match) ? replacement : value;
    }

    private sealed class ObservingFilter : IRedactionFilter
    {
        private readonly List<object?> sink;

        public ObservingFilter(List<object?> sink) => this.sink = sink;

        public object? Filter(RedactionContext context, object? value)
        {
            sink.Add(value);
            return value;
        }
    }

    private sealed class IdentityFilter : IRedactionFilter
    {
        public object? Filter(RedactionContext context, object? value) => value;
    }
}
