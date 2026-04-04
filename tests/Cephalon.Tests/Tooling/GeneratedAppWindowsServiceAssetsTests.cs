namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppWindowsServiceAssetsTests
{
    [Fact]
    public void GeneratedAppWindowsServiceAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "windows-service-deployment.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-windows-service.ps1");

        Assert.True(File.Exists(guidePath), "Expected the Windows Service deployment guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app Windows Service validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/windows-service", guide, StringComparison.Ordinal);
        Assert.Contains("install-service.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("sc.exe create", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("PublishProfile=CephalonFolder", validationScript, StringComparison.Ordinal);
        Assert.Contains("builder.Host.UseWindowsService();", validationScript, StringComparison.Ordinal);
        Assert.Contains("sc.exe create", validationScript, StringComparison.Ordinal);
        Assert.Contains("--contentRoot", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app Windows Service validation completed successfully.", validationScript, StringComparison.Ordinal);
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
