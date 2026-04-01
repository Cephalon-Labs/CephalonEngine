using Cephalon.Cli;
using Cephalon.Cli.Commands;
using System.Text.Json.Nodes;

namespace Cephalon.Tests.Tooling;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task RunAsyncGeneratesAppFromBlueprintCommand()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "new",
                    "Acme.Store",
                    "--blueprint", "Microservice",
                    "--module", "Orders",
                    "--feature", "Checkout",
                    "--transport", "Grpc",
                    "--transport", "JsonRpc",
                    "--technology", "AgenticWorkloads",
                    "--output", outputPath,
                    "--package-version", "3.2.0-preview"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "Acme.Store.slnx")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Acme.Store.Service", "Program.cs")));

            var packageProps = await File.ReadAllTextAsync(Path.Combine(outputPath, "Directory.Packages.props"));
            Assert.Contains("Cephalon.AspNetCore.Grpc", packageProps, StringComparison.Ordinal);
            Assert.Contains("Version=\"3.2.0-preview\"", packageProps, StringComparison.Ordinal);

            var settings = await File.ReadAllTextAsync(Path.Combine(outputPath, "src", "Acme.Store.Service", "appsettings.json"));
            Assert.Contains("\"Microservice\"", settings, StringComparison.Ordinal);
            Assert.Contains("\"gRPC\"", settings, StringComparison.Ordinal);
            Assert.Contains("\"JSON-RPC\"", settings, StringComparison.Ordinal);
            Assert.Contains("\"Agentic Workloads\"", settings, StringComparison.Ordinal);

            Assert.Contains("Generated 'Acme.Store'", stdout.ToString(), StringComparison.Ordinal);
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
    public async Task RunAsyncPublishesReferenceDocsFromCli()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-reference-docs-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", GetRepositoryRoot(),
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--assembly", "Cephalon.Agentics"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "browse.html")));
            Assert.True(File.Exists(Path.Combine(outputPath, "members.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "reference-manifest.json")));
            Assert.Contains("Published 11 reference doc files", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Assemblies: Cephalon.Engine, Cephalon.Agentics", stdout.ToString(), StringComparison.Ordinal);
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
    public async Task RunAsyncPublishesReferenceDocsAndEnablesHostingFromCli()
    {
        var rootPath = GetRepositoryRoot();
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-publish-hosting-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(workspacePath, "published-docs");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", rootPath,
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--enable-hosting",
                    "--appsettings", appSettingsPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(outputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(outputPath, "browse.html")));

            var rootObject = await LoadJsonObjectAsync(appSettingsPath);
            var referenceDocs = Assert.IsType<JsonObject>(rootObject["ReferenceDocs"]);

            Assert.Equal(true, referenceDocs["Enabled"]?.GetValue<bool>());
            Assert.Equal("/reference", referenceDocs["RoutePrefix"]?.GetValue<string>());
            Assert.Equal("..\\..\\published-docs", referenceDocs["DirectoryPath"]?.GetValue<string>());
            Assert.Equal("browse.html", referenceDocs["DefaultDocument"]?.GetValue<string>());
            Assert.Contains("Published", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Enabled hosted reference docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishesReferenceDocsAndValidatesHostingFromCli()
    {
        var rootPath = GetRepositoryRoot();
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-publish-validate-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(workspacePath, "published-docs");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", rootPath,
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--enable-hosting",
                    "--validate-hosting",
                    "--appsettings", appSettingsPath,
                    "--host-url", "https://localhost:7235"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("Published", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Enabled hosted reference docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("ReferenceDocs hosting is ready", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Hosted browser URL: https://localhost:7235/reference/browse.html", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishOpenUsesGeneratedBrowseHtml()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-reference-docs-open-{Guid.NewGuid():N}");
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Uri? launchedUri = null;

        DocumentationLauncher.OpenOverride = (target, _) =>
        {
            launchedUri = target;
            return Task.CompletedTask;
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", GetRepositoryRoot(),
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--open"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.NotNull(launchedUri);
            Assert.True(launchedUri!.IsFile);
            Assert.EndsWith("browse.html", launchedUri.LocalPath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Opened reference docs:", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            DocumentationLauncher.OpenOverride = null;

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishOpenUsesHostedUrlWhenRequested()
    {
        var rootPath = GetRepositoryRoot();
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-open-hosted-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(workspacePath, "published-docs");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Uri? launchedUri = null;

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        DocumentationLauncher.OpenOverride = (target, _) =>
        {
            launchedUri = target;
            return Task.CompletedTask;
        };

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "publish",
                    "--root", rootPath,
                    "--output", outputPath,
                    "--configuration", GetCurrentBuildConfiguration(),
                    "--assembly", "Cephalon.Engine",
                    "--enable-hosting",
                    "--appsettings", appSettingsPath,
                    "--open",
                    "--host-url", "https://localhost:7235"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.NotNull(launchedUri);
            Assert.Equal("https://localhost:7235/reference/browse.html", launchedUri!.AbsoluteUri);
            Assert.Contains("Opened reference docs: https://localhost:7235/reference/browse.html", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            DocumentationLauncher.OpenOverride = null;

            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncEnablesHostedReferenceDocsFromCli()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-enable-hosting-{Guid.NewGuid():N}");
        var appSettingsDirectory = Path.Combine(rootPath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        Directory.CreateDirectory(Path.Combine(rootPath, "docs", "reference"));
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "Engine": {
                "Blueprint": "ModularMonolith"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "enable-hosting",
                    "--appsettings", appSettingsPath,
                    "--root", rootPath
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);

            var rootObject = await LoadJsonObjectAsync(appSettingsPath);
            var referenceDocs = Assert.IsType<JsonObject>(rootObject["ReferenceDocs"]);

            Assert.Equal(true, referenceDocs["Enabled"]?.GetValue<bool>());
            Assert.Equal("/reference", referenceDocs["RoutePrefix"]?.GetValue<string>());
            Assert.Equal("..\\..\\docs\\reference", referenceDocs["DirectoryPath"]?.GetValue<string>());
            Assert.Equal("browse.html", referenceDocs["DefaultDocument"]?.GetValue<string>());
            Assert.Contains("Enabled hosted reference docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncEnableHostingPreservesExistingSettingsUnlessOverridden()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-preserve-hosting-{Guid.NewGuid():N}");
        var appSettingsDirectory = Path.Combine(rootPath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "ReferenceDocs": {
                "Enabled": false,
                "RoutePrefix": "/docs",
                "DirectoryPath": "..\\..\\custom-reference",
                "DefaultDocument": "landing.html"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "enable-hosting",
                    "--appsettings", appSettingsPath,
                    "--root", rootPath,
                    "--default-document", "browse.html"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);

            var rootObject = await LoadJsonObjectAsync(appSettingsPath);
            var referenceDocs = Assert.IsType<JsonObject>(rootObject["ReferenceDocs"]);

            Assert.Equal(true, referenceDocs["Enabled"]?.GetValue<bool>());
            Assert.Equal("/docs", referenceDocs["RoutePrefix"]?.GetValue<string>());
            Assert.Equal("..\\..\\custom-reference", referenceDocs["DirectoryPath"]?.GetValue<string>());
            Assert.Equal("browse.html", referenceDocs["DefaultDocument"]?.GetValue<string>());
            Assert.Contains("DefaultDocument='browse.html'", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncPublishEnableHostingRequiresAppSettings()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "docs",
                "publish",
                "--root", GetRepositoryRoot(),
                "--enable-hosting"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Option '--enable-hosting' requires '--appsettings <path>'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncPublishValidateHostingRequiresAppSettings()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "docs",
                "publish",
                "--root", GetRepositoryRoot(),
                "--validate-hosting"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Option '--validate-hosting' requires '--appsettings <path>'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncPublishHostUrlRequiresOpen()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            [
                "docs",
                "publish",
                "--root", GetRepositoryRoot(),
                "--enable-hosting",
                "--appsettings", "appsettings.json",
                "--host-url", "https://localhost:7235"
            ],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Option '--host-url' requires '--open' or '--validate-hosting'.", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncValidateHostingSucceedsWhenReferenceDocsAreReady()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-validate-hosting-{Guid.NewGuid():N}");
        var docsDirectory = Path.Combine(workspacePath, "docs", "reference");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(docsDirectory);
        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(Path.Combine(docsDirectory, "browse.html"), "<html></html>");
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "ReferenceDocs": {
                "Enabled": true,
                "RoutePrefix": "/reference",
                "DirectoryPath": "..\\..\\docs\\reference",
                "DefaultDocument": "browse.html"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "validate-hosting",
                    "--appsettings", appSettingsPath,
                    "--host-url", "https://localhost:7235"
                ],
                stdout,
                stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("ReferenceDocs hosting is ready", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Hosted browser URL: https://localhost:7235/reference/browse.html", stdout.ToString(), StringComparison.Ordinal);
            Assert.Contains("Engine reference-docs URL: https://localhost:7235/engine/reference-docs", stdout.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncValidateHostingFailsWhenDefaultDocumentIsMissing()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-validate-hosting-missing-{Guid.NewGuid():N}");
        var docsDirectory = Path.Combine(workspacePath, "docs", "reference");
        var appSettingsDirectory = Path.Combine(workspacePath, "src", "Acme.Store.Service");
        var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        Directory.CreateDirectory(docsDirectory);
        Directory.CreateDirectory(appSettingsDirectory);
        await File.WriteAllTextAsync(
            appSettingsPath,
            """
            {
              "ReferenceDocs": {
                "Enabled": true,
                "RoutePrefix": "/reference",
                "DirectoryPath": "..\\..\\docs\\reference",
                "DefaultDocument": "browse.html"
              }
            }
            """);

        try
        {
            var exitCode = await CliApplication.RunAsync(
                [
                    "docs",
                    "validate-hosting",
                    "--appsettings", appSettingsPath
                ],
                stdout,
                stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("does not exist", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsyncShowsErrorForUnknownOption()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["new", "Acme.Store", "--unknown"],
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown option '--unknown'.", stderr.ToString(), StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "Cephalon.slnx");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find repository root from '{AppContext.BaseDirectory}'.");
    }

    private static string GetCurrentBuildConfiguration()
    {
        return AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
    }

    private static async Task<JsonObject> LoadJsonObjectAsync(string path)
    {
        var parsed = JsonNode.Parse(await File.ReadAllTextAsync(path));
        return Assert.IsType<JsonObject>(parsed);
    }
}
