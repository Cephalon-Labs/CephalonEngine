namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppAppServiceAssetsTests
{
    [Fact]
    public void GeneratedAppAppServiceAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "azure-app-service-deployment.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-app-service.ps1");

        Assert.True(File.Exists(guidePath), "Expected the Azure App Service deployment guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app Azure App Service validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/azure-app-service", guide, StringComparison.Ordinal);
        Assert.Contains("deploy-zip.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", guide, StringComparison.Ordinal);
        Assert.Contains("az webapp deploy", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("PublishProfile=CephalonFolder", validationScript, StringComparison.Ordinal);
        Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", validationScript, StringComparison.Ordinal);
        Assert.Contains("az webapp deploy", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app Azure App Service validation completed successfully.", validationScript, StringComparison.Ordinal);
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
