namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppIisAssetsTests
{
    [Fact]
    public void GeneratedAppIisAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "iis-deployment.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-iis.ps1");

        Assert.True(File.Exists(guidePath), "Expected the IIS deployment guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app IIS validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/iis", guide, StringComparison.Ordinal);
        Assert.Contains("install-site.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("AspNetCoreModuleV2", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("PublishProfile=CephalonFolder", validationScript, StringComparison.Ordinal);
        Assert.Contains("AspNetCoreModuleV2", validationScript, StringComparison.Ordinal);
        Assert.Contains("processPath=\"dotnet\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app IIS validation completed successfully.", validationScript, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
    }
}
