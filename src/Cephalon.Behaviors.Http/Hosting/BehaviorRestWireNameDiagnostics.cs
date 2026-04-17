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
}
