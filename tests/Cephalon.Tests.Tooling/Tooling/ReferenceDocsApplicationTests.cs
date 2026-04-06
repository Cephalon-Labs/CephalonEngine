using Cephalon.ReferenceDocs;

namespace Cephalon.Tests.Tooling;

public sealed class ReferenceDocsApplicationTests
{
    [Fact]
    public async Task RunAsyncWritesReferenceDocsToOutputDirectory()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-app-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        try
        {
            var exitCode = await ReferenceDocsApplication.RunAsync(
                [
                    "--root", GetRepositoryRoot(),
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--assembly", "Cephalon.Agentics"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "index.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "namespaces.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "types.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "members.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "reference-manifest.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "browse.html")));
            Assert.True(File.Exists(Path.Combine(outputPath, "reference-browser.css")));
            Assert.True(File.Exists(Path.Combine(outputPath, "reference-browser.js")));
            Assert.True(File.Exists(Path.Combine(outputPath, "cephalon-engine.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "cephalon-agentics.md")));
            Assert.Contains("Generated 11 reference doc files", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncShowsHelpForHelpFlag()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ReferenceDocsApplication.RunAsync(
            ["--help"],
            stdout,
            stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Reference docs generator", stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr.ToString());
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

    private static string GetCurrentBuildConfiguration()
    {
        return AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
    }
}
