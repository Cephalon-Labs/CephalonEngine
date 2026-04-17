using Cephalon.Behaviors.Http.Abstractions;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestWireNameDiagnostics
{
    internal static string SupportedMethodWireNames { get; } = string.Join(
        ", ",
        Enum.GetValues<BehaviorRestMethod>()
            .Where(static method => method != BehaviorRestMethod.Unspecified)
            .Select(static method => method.GetWireName()));

    internal static string SupportedBindingSourceWireNames { get; } = string.Join(
        ", ",
        Enum.GetValues<BehaviorRestBindingSource>()
            .Where(static source => source != BehaviorRestBindingSource.Unspecified)
            .Select(static source => source.GetWireName()));

    internal static string DescribeMethodSupport()
        => $"Supported behavior REST method wire names: {SupportedMethodWireNames}.";

    internal static string DescribeBindingSourceSupport()
        => $"Supported behavior REST binding-source wire names: {SupportedBindingSourceWireNames}.";

    internal static string GetWireName(BehaviorRestMethod method)
        => method.GetWireName();

    internal static string GetWireName(RestBehaviorHttpMethod method)
    {
        return method switch
        {
            RestBehaviorHttpMethod.Get => BehaviorRestMethod.Get.GetWireName(),
            RestBehaviorHttpMethod.Post => BehaviorRestMethod.Post.GetWireName(),
            RestBehaviorHttpMethod.Put => BehaviorRestMethod.Put.GetWireName(),
            RestBehaviorHttpMethod.Patch => BehaviorRestMethod.Patch.GetWireName(),
            RestBehaviorHttpMethod.Delete => BehaviorRestMethod.Delete.GetWireName(),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "A supported REST behavior HTTP method is required.")
        };
    }
}
