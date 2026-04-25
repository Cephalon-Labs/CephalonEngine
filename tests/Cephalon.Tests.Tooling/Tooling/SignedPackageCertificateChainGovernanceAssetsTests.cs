namespace Cephalon.Tests.Tooling;

public sealed class SignedPackageCertificateChainGovernanceAssetsTests
{
    [Fact]
    public void SignedPackageCertificateChainGovernanceAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var rootReadmePath = Path.Combine(repositoryRoot, "README.md");
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var packagePublishingPath = Path.Combine(repositoryRoot, "docs", "package-publishing.md");
        var packageLifecyclePath = Path.Combine(repositoryRoot, "docs", "external-package-lifecycle.md");
        var cliComponentDocPath = Path.Combine(repositoryRoot, "docs", "components", "cli.md");
        var cliPackageReadmePath = Path.Combine(repositoryRoot, "src", "Cephalon.Cli", "PACKAGE.md");
        var templatePackReadmePath = Path.Combine(repositoryRoot, "templates", "Cephalon.TemplatePack", "PACKAGE.md");
        var governanceScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-signed-package-governance.ps1");
        var certificateChainScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-signed-package-certificate-chain-governance.ps1");

        Assert.True(File.Exists(rootReadmePath), "Expected the repository README.");
        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(packagePublishingPath), "Expected the package publishing guide.");
        Assert.True(File.Exists(packageLifecyclePath), "Expected the external package lifecycle guide.");
        Assert.True(File.Exists(cliComponentDocPath), "Expected the CLI component guide.");
        Assert.True(File.Exists(cliPackageReadmePath), "Expected the CLI package README.");
        Assert.True(File.Exists(templatePackReadmePath), "Expected the template-pack README.");
        Assert.True(File.Exists(governanceScriptPath), "Expected the signed package governance validation script.");
        Assert.True(File.Exists(certificateChainScriptPath), "Expected the certificate-chain signed package governance validation script.");

        var rootReadme = File.ReadAllText(rootReadmePath);
        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var packagePublishing = File.ReadAllText(packagePublishingPath);
        var packageLifecycle = File.ReadAllText(packageLifecyclePath);
        var cliComponentDoc = File.ReadAllText(cliComponentDocPath);
        var cliPackageReadme = File.ReadAllText(cliPackageReadmePath);
        var templatePackReadme = File.ReadAllText(templatePackReadmePath);
        var governanceScript = File.ReadAllText(governanceScriptPath);
        var certificateChainScript = File.ReadAllText(certificateChainScriptPath);

        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", packagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-signed-package-certificate-chain-governance.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("SignatureTrustMode", certificateChainScript, StringComparison.Ordinal);
        Assert.Contains("CertificateChain", certificateChainScript, StringComparison.Ordinal);
        Assert.Contains("TrustedSignatureCertificates", governanceScript, StringComparison.Ordinal);
        Assert.Contains("TrustedSignatureCertificateAuthorities", governanceScript, StringComparison.Ordinal);
        Assert.Contains("trusted-certificate-chain", governanceScript, StringComparison.Ordinal);
        Assert.Contains("signatureCertificateThumbprint", governanceScript, StringComparison.Ordinal);
        Assert.Contains("certificateThumbprint", governanceScript, StringComparison.Ordinal);
        Assert.Contains("/engine/trust-policy", governanceScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", governanceScript, StringComparison.Ordinal);
        Assert.Contains("/api/operations/status", governanceScript, StringComparison.Ordinal);
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
