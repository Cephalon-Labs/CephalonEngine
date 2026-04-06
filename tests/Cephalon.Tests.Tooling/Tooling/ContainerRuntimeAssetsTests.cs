namespace Cephalon.Tests.Tooling;

public sealed class ContainerRuntimeAssetsTests
{
    [Fact]
    public void ModularMonolithContainerAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var sampleRoot = Path.Combine(repositoryRoot, "samples", "Cephalon.Sample.ModularMonolith");
        var dockerignorePath = Path.Combine(repositoryRoot, ".dockerignore");
        var dockerfilePath = Path.Combine(sampleRoot, "Dockerfile");
        var composePath = Path.Combine(sampleRoot, "compose.yaml");
        var composePackagesPath = Path.Combine(sampleRoot, "compose.packages.yaml");
        var collectorConfigPath = Path.Combine(sampleRoot, "otel-collector-config.yaml");
        var sampleReadmePath = Path.Combine(sampleRoot, "README.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-container-runtime.ps1");
        var pluginsPlaceholderPath = Path.Combine(sampleRoot, "plugins", ".gitkeep");

        Assert.True(File.Exists(dockerignorePath), "Expected a repo-level .dockerignore for sample container builds.");
        Assert.True(File.Exists(dockerfilePath), "Expected the modular monolith sample Dockerfile.");
        Assert.True(File.Exists(composePath), "Expected the modular monolith sample compose file.");
        Assert.True(File.Exists(composePackagesPath), "Expected the modular monolith sample package override compose file.");
        Assert.True(File.Exists(collectorConfigPath), "Expected the modular monolith sample collector config.");
        Assert.True(File.Exists(sampleReadmePath), "Expected the modular monolith sample README.");
        Assert.True(File.Exists(validationScriptPath), "Expected the optional container runtime validation script.");
        Assert.True(File.Exists(pluginsPlaceholderPath), "Expected the modular monolith sample plugins placeholder.");

        var dockerignore = File.ReadAllText(dockerignorePath);
        var dockerfile = File.ReadAllText(dockerfilePath);
        var compose = File.ReadAllText(composePath);
        var composePackages = File.ReadAllText(composePackagesPath);
        var collectorConfig = File.ReadAllText(collectorConfigPath);
        var sampleReadme = File.ReadAllText(sampleReadmePath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("samples/Cephalon.Sample.ModularMonolith/plugins/*", dockerignore, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Sample.ModularMonolith.csproj", dockerfile, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Sample.ModularMonolith.dll", dockerfile, StringComparison.Ordinal);
        Assert.Contains("dockerfile: samples/Cephalon.Sample.ModularMonolith/Dockerfile", compose, StringComparison.Ordinal);
        Assert.Contains("DOTNET_ENVIRONMENT: Container", compose, StringComparison.Ordinal);
        Assert.Contains("http://otel-collector:4318", compose, StringComparison.Ordinal);
        Assert.Contains("otel/opentelemetry-collector-contrib", compose, StringComparison.Ordinal);
        Assert.Contains("Engine__Discovery__PackageDirectories__0: /app/plugins", composePackages, StringComparison.Ordinal);
        Assert.Contains("Engine__PackagePolicy__RequireVersion: \"true\"", composePackages, StringComparison.Ordinal);
        Assert.Contains("health_check", collectorConfig, StringComparison.Ordinal);
        Assert.Contains("debug", collectorConfig, StringComparison.Ordinal);
        Assert.Contains("docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml up --build", sampleReadme, StringComparison.Ordinal);
        Assert.Contains("compose.packages.yaml", sampleReadme, StringComparison.Ordinal);
        Assert.Contains("otel-collector", sampleReadme, StringComparison.Ordinal);
        Assert.Contains("/api/operations/status", sampleReadme, StringComparison.Ordinal);
        Assert.Contains("samples\\Cephalon.Sample.ModularMonolith", validationScript, StringComparison.Ordinal);
        Assert.Contains("Container runtime validation completed successfully.", validationScript, StringComparison.Ordinal);
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
