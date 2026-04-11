namespace Cephalon.Scaffolding.Generation;

internal static class PackageVersionCatalog
{
    private static readonly string[] TestInfrastructurePackages =
    [
        "coverlet.collector",
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.runner.visualstudio"
    ];

    private static readonly Dictionary<string, string> Versions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["coverlet.collector"] = "8.0.1",
            ["Microsoft.Extensions.Hosting.WindowsServices"] = "10.0.5",
            ["Microsoft.NET.Test.Sdk"] = "18.3.0",
            ["Serilog.Sinks.Console"] = "6.1.1",
            ["xunit"] = "2.9.3",
            ["xunit.runner.visualstudio"] = "3.1.5"
        };

    public static IReadOnlyList<string> GetTestInfrastructurePackages()
    {
        return TestInfrastructurePackages;
    }

    public static string Resolve(string package, string cephalonPackageVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(cephalonPackageVersion);

        if (package.StartsWith("Cephalon.", StringComparison.OrdinalIgnoreCase))
        {
            return cephalonPackageVersion.Trim();
        }

        if (Versions.TryGetValue(package.Trim(), out var version))
        {
            return version;
        }

        throw new InvalidOperationException($"No package version was defined for '{package}'.");
    }
}
