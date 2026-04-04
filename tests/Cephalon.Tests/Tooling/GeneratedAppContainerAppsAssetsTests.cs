namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppContainerAppsAssetsTests
{
    [Fact]
    public void GeneratedAppContainerAppsAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "azure-container-apps-deployment.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-container-apps.ps1");

        Assert.True(File.Exists(guidePath), "Expected the Azure Container Apps deployment guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app Azure Container Apps validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/azure-container-apps", guide, StringComparison.Ordinal);
        Assert.Contains("deploy-up.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("az containerapp up", guide, StringComparison.Ordinal);
        Assert.Contains("--source", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-Process -FileName \"docker\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"build\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("az containerapp up", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app Azure Container Apps validation completed successfully.", validationScript, StringComparison.Ordinal);
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
