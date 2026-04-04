namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppSystemdAssetsTests
{
    [Fact]
    public void GeneratedAppSystemdAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "linux-systemd-deployment.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-systemd.ps1");

        Assert.True(File.Exists(guidePath), "Expected the Linux systemd deployment guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app systemd validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/linux/systemd", guide, StringComparison.Ordinal);
        Assert.Contains("systemd-analyze verify", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-systemd.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("PublishProfile=CephalonFolder", validationScript, StringComparison.Ordinal);
        Assert.Contains("systemd-analyze verify", validationScript, StringComparison.Ordinal);
        Assert.Contains("wsl.exe", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app Linux systemd validation completed successfully.", validationScript, StringComparison.Ordinal);
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
