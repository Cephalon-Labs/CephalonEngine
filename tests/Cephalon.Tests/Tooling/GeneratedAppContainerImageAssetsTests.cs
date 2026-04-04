namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppContainerImageAssetsTests
{
    [Fact]
    public void GeneratedAppContainerImageAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "container-image-publishing.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-container-image.ps1");

        Assert.True(File.Exists(guidePath), "Expected the container-image publishing guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app container-image validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/container-image", guide, StringComparison.Ordinal);
        Assert.Contains("publish-image.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("docker push", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("registry:2", validationScript, StringComparison.Ordinal);
        Assert.Contains("docker push", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app container-image validation completed successfully.", validationScript, StringComparison.Ordinal);
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
