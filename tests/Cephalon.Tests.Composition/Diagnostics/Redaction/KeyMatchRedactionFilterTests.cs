using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Defaults;

namespace Cephalon.Tests.Diagnostics.Redaction;

public sealed class KeyMatchRedactionFilterTests
{
    [Fact]
    public void Filter_WhenAttributeKeyMatches_ReturnsDefaultReplacement()
    {
        var filter = new KeyMatchRedactionFilter(["http.request.header.authorization"]);
        var context = new RedactionContext(
            ActivitySourceName: "Cephalon.AspNetCore",
            MeterName: null,
            AttributeKey: "http.request.header.authorization",
            LoggerCategory: null);

        var result = filter.Filter(context, "Bearer abc123");

        Assert.Equal(KeyMatchRedactionFilter.DefaultReplacement, result);
    }

    [Fact]
    public void Filter_WhenAttributeKeyDoesNotMatch_ReturnsValueUnchanged()
    {
        var filter = new KeyMatchRedactionFilter(["http.request.header.authorization"]);
        var context = new RedactionContext(
            ActivitySourceName: "Cephalon.AspNetCore",
            MeterName: null,
            AttributeKey: "http.request.method",
            LoggerCategory: null);

        var result = filter.Filter(context, "GET");

        Assert.Equal("GET", result);
    }

    [Fact]
    public void Filter_KeyComparisonIsOrdinalCaseInsensitive()
    {
        var filter = new KeyMatchRedactionFilter(["http.request.header.AUTHORIZATION"]);
        var context = new RedactionContext(
            ActivitySourceName: null,
            MeterName: null,
            AttributeKey: "http.request.header.authorization",
            LoggerCategory: null);

        var result = filter.Filter(context, "Bearer abc123");

        Assert.Equal(KeyMatchRedactionFilter.DefaultReplacement, result);
    }

    [Fact]
    public void Filter_RespectsCustomReplacement()
    {
        var replacement = new byte[] { 0, 0, 0, 0 };
        var filter = new KeyMatchRedactionFilter(["cephalon.tenant.secret"], replacement);
        var context = new RedactionContext(
            ActivitySourceName: null,
            MeterName: null,
            AttributeKey: "cephalon.tenant.secret",
            LoggerCategory: null);

        var result = filter.Filter(context, new byte[] { 1, 2, 3, 4 });

        Assert.Same(replacement, result);
    }

    [Fact]
    public void Filter_ShortCircuitsOnAttributeKey_DoesNotInspectValue()
    {
        var filter = new KeyMatchRedactionFilter(["sensitive.key"]);
        var context = new RedactionContext(
            ActivitySourceName: null,
            MeterName: null,
            AttributeKey: "sensitive.key",
            LoggerCategory: null);

        var result = filter.Filter(context, value: null);

        Assert.Equal(KeyMatchRedactionFilter.DefaultReplacement, result);
    }

    [Fact]
    public void Filter_AcceptsMultipleBannedKeys()
    {
        var filter = new KeyMatchRedactionFilter(
        [
            "http.request.header.authorization",
            "http.request.header.cookie",
            "cephalon.tenant.secret",
        ]);

        foreach (var key in new[] { "http.request.header.authorization", "http.request.header.cookie", "cephalon.tenant.secret" })
        {
            var context = new RedactionContext(null, null, key, null);
            Assert.Equal(KeyMatchRedactionFilter.DefaultReplacement, filter.Filter(context, "value"));
        }

        var allowed = new RedactionContext(null, null, "http.request.method", null);
        Assert.Equal("GET", filter.Filter(allowed, "GET"));
    }

    [Fact]
    public void Constructor_ThrowsOnNullBannedKeys()
    {
        Assert.Throws<ArgumentNullException>(
            () => new KeyMatchRedactionFilter(bannedAttributeKeys: null!));
    }

    [Fact]
    public void DefaultReplacement_IsTheRedactedSentinel()
    {
        Assert.Equal("[REDACTED]", KeyMatchRedactionFilter.DefaultReplacement);
    }
}
