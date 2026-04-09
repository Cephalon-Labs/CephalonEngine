using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Tests.Composition.Behaviors;

public sealed class BehaviorResultTests
{
    [Fact]
    public void RawPayloadConvertsToSuccessfulBehaviorResult()
    {
        BehaviorResult<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.True(result.HasValue);
        Assert.Equal(BehaviorResultStatus.Ok, result.Status);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void NotFoundResultCarriesStructuredFaultState()
    {
        var result = BehaviorResult.NotFound<string>(
            "cart.not_found",
            "Cart 'cart-123' was not found.",
            new BehaviorFault
            {
                Code = "cart.not_found",
                Message = "Cart 'cart-123' was not found.",
                Severity = BehaviorFaultSeverity.Warning,
                Details = "The aggregate stream does not exist."
            });

        Assert.False(result.IsSuccess);
        Assert.False(result.HasValue);
        Assert.Equal(BehaviorResultStatus.NotFound, result.Status);
        Assert.Equal("cart.not_found", result.Code);
        Assert.NotNull(result.Fault);
        Assert.Equal(BehaviorFaultSeverity.Warning, result.Fault!.Severity);
    }
}
