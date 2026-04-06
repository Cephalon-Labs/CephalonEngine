namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppKubernetesAssetsTests
{
    [Fact]
    public void GeneratedAppKubernetesAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "docs", "kubernetes-deployment.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-kubernetes.ps1");

        Assert.True(File.Exists(guidePath), "Expected the Kubernetes deployment guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app Kubernetes validation script.");

        var guide = File.ReadAllText(guidePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("deploy/kubernetes", guide, StringComparison.Ordinal);
        Assert.Contains("apply.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("kubectl kustomize", guide, StringComparison.Ordinal);
        Assert.Contains("ClusterIP", guide, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-Process -FileName \"docker\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("kubectl kustomize --help", validationScript, StringComparison.Ordinal);
        Assert.Contains("kind: Deployment", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app Kubernetes validation completed successfully.", validationScript, StringComparison.Ordinal);
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
