namespace Cephalon.Tests.Tooling;

public sealed class SignedPackageGovernanceAssetsTests
{
    [Fact]
    public void SignedPackageGovernanceAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var packagePublishingPath = Path.Combine(repositoryRoot, "docs", "package-publishing.md");
        var packageLifecyclePath = Path.Combine(repositoryRoot, "docs", "external-package-lifecycle.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-signed-package-governance.ps1");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(packagePublishingPath), "Expected the package publishing guide.");
        Assert.True(File.Exists(packageLifecyclePath), "Expected the external package lifecycle guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the signed package governance validation script.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var packagePublishing = File.ReadAllText(packagePublishingPath);
        var packageLifecycle = File.ReadAllText(packageLifecyclePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("validate-signed-package-governance.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-governance.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-governance.ps1", packagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-governance.ps1", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("\"tool\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"install\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli", validationScript, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", validationScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TrustedSignaturePublicKeys", validationScript, StringComparison.Ordinal);
        Assert.Contains("RequireSignatureVerification", validationScript, StringComparison.Ordinal);
        Assert.Contains("RequireSignatureValue", validationScript, StringComparison.Ordinal);
        Assert.Contains("RequireSignatureKeyId", validationScript, StringComparison.Ordinal);
        Assert.Contains("RequireSignatureFingerprint", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/packages", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/package-policy", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/trust-policy", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("/api/operations/status", validationScript, StringComparison.Ordinal);
        Assert.Contains("trusted-public-key", validationScript, StringComparison.Ordinal);
        Assert.Contains("tampered", validationScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Signed package governance validation completed successfully.", validationScript, StringComparison.Ordinal);
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
