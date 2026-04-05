namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppPublishAssetsTests
{
    [Fact]
    public void GeneratedAppPublishAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var publishGuidePath = Path.Combine(repositoryRoot, "docs", "generated-app-publishing.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-publish.ps1");

        Assert.True(File.Exists(publishGuidePath), "Expected the generated-app publishing guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app publish validation script.");

        var publishGuide = File.ReadAllText(publishGuidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("CephalonFolder.pubxml", publishGuide, StringComparison.Ordinal);
        Assert.Contains("dotnet publish", publishGuide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", publishGuide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("PublishProfile=CephalonFolder", validationScript, StringComparison.Ordinal);
        Assert.Contains("/health/ready", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app publish validation completed successfully.", validationScript, StringComparison.Ordinal);
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
