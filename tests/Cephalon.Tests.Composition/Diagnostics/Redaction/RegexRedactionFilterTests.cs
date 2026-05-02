using System.Text.RegularExpressions;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Defaults;

namespace Cephalon.Tests.Diagnostics.Redaction;

public sealed class RegexRedactionFilterTests
{
    private static readonly RedactionContext SampleContext = new(
        ActivitySourceName: "Cephalon.AspNetCore",
        MeterName: null,
        AttributeKey: "http.request.url.full",
        LoggerCategory: null);

    [Fact]
    public void Filter_WhenStringMatchesPattern_ReplacesMatchedSubstrings()
    {
        var filter = new RegexRedactionFilter(new Regex(@"Bearer\s+\S+"));

        var result = filter.Filter(SampleContext, "Authorization: Bearer abc123 trailing");

        Assert.Equal($"Authorization: {RegexRedactionFilter.DefaultReplacement} trailing", result);
    }

    [Fact]
    public void Filter_WhenStringDoesNotMatch_ReturnsOriginalString()
    {
        var filter = new RegexRedactionFilter(new Regex(@"Bearer\s+\S+"));

        var result = filter.Filter(SampleContext, "no secret here");

        Assert.Equal("no secret here", result);
    }

    [Fact]
    public void Filter_WhenValueIsNotString_ReturnsValueUnchanged()
    {
        var filter = new RegexRedactionFilter(new Regex(@"\d+"));
        var nonString = new[] { 1, 2, 3 };

        var result = filter.Filter(SampleContext, nonString);

        Assert.Same(nonString, result);
    }

    [Fact]
    public void Filter_WhenValueIsNull_ReturnsNull()
    {
        var filter = new RegexRedactionFilter(new Regex(@"\d+"));

        var result = filter.Filter(SampleContext, value: null);

        Assert.Null(result);
    }

    [Fact]
    public void Filter_WhenValueIsEmptyString_ReturnsEmptyString()
    {
        var filter = new RegexRedactionFilter(new Regex(@"\d+"));

        var result = filter.Filter(SampleContext, string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Filter_RespectsCustomReplacement()
    {
        var filter = new RegexRedactionFilter(new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}"), "[CC]");

        var result = filter.Filter(SampleContext, "card=4242-4242-4242-4242");

        Assert.Equal("card=[CC]", result);
    }

    [Fact]
    public void Filter_ReplacesAllMatches_NotJustTheFirst()
    {
        var filter = new RegexRedactionFilter(new Regex(@"\d+"));

        var result = filter.Filter(SampleContext, "id=42 and ref=99");

        Assert.Equal($"id={RegexRedactionFilter.DefaultReplacement} and ref={RegexRedactionFilter.DefaultReplacement}", result);
    }

    [Fact]
    public void Filter_ReusesCompiledRegexAcrossCalls()
    {
        var pattern = new Regex(@"secret-\w+", RegexOptions.Compiled);
        var filter = new RegexRedactionFilter(pattern);

        var first = filter.Filter(SampleContext, "value=secret-foo");
        var second = filter.Filter(SampleContext, "value=secret-bar");

        Assert.Equal($"value={RegexRedactionFilter.DefaultReplacement}", first);
        Assert.Equal($"value={RegexRedactionFilter.DefaultReplacement}", second);
    }

    [Fact]
    public void Constructor_ThrowsOnNullPattern()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RegexRedactionFilter(pattern: null!));
    }

    [Fact]
    public void DefaultReplacement_IsTheRedactedSentinel()
    {
        Assert.Equal("[REDACTED]", RegexRedactionFilter.DefaultReplacement);
    }
}
