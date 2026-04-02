using System.IO.Compression;
using System.Text.Json;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Tooling;

public sealed class TemplatePackTests
{
    [Fact]
    public void TemplatePackContainsExpectedBlueprintTemplates()
    {
        var templateRoot = FindRepositoryDirectory(Path.Combine("templates", "Cephalon.TemplatePack", "templates"));
        var expectedTemplates = new[]
        {
            ("cephalon-modular-monolith", "Cephalon.Templates.ModularMonolith", "cephalon-monolith", "CephalonTemplateApp"),
            ("cephalon-modular-vertical-slice", "Cephalon.Templates.ModularVerticalSlice", "cephalon-slice", "CephalonTemplateApp"),
            ("cephalon-microservice", "Cephalon.Templates.Microservice", "cephalon-microservice", "CephalonTemplateApp"),
            ("cephalon-module", "Cephalon.Templates.Module", "cephalon-module", "CephalonTemplateModule"),
            ("cephalon-rest-module", "Cephalon.Templates.RestModule", "cephalon-rest-module", "CephalonTemplateModule")
        };

        foreach (var (folderName, identity, shortName, sourceName) in expectedTemplates)
        {
            var templateJsonPath = Path.Combine(templateRoot, folderName, ".template.config", "template.json");
            Assert.True(File.Exists(templateJsonPath), $"Template config '{templateJsonPath}' was not found.");

            using var document = JsonDocument.Parse(File.ReadAllText(templateJsonPath));
            Assert.Equal(identity, document.RootElement.GetProperty("identity").GetString());
            Assert.Equal(shortName, document.RootElement.GetProperty("shortName").GetString());
            Assert.Equal(sourceName, document.RootElement.GetProperty("sourceName").GetString());
        }
    }

    [Fact]
    public void TemplatePackProjectPacksTemplatesIntoNuGetContentFolder()
    {
        var templateProjectPath = FindRepositoryFile(Path.Combine("templates", "Cephalon.TemplatePack", "Cephalon.TemplatePack.csproj"));
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-pack-{Guid.NewGuid():N}");

        Directory.CreateDirectory(outputPath);

        try
        {
            var result = RunProcess(
                "dotnet",
                $"pack \"{templateProjectPath}\" -c Debug -o \"{outputPath}\"",
                workingDirectory: Path.GetDirectoryName(templateProjectPath)!);

            Assert.Equal(0, result.ExitCode);

            var packagePath = Directory.GetFiles(outputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

            using var package = ZipFile.OpenRead(packagePath);
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-module/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-rest-module/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-module/cephalon.package.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-rest-module/cephalon.package.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("PACKAGE.md", StringComparison.OrdinalIgnoreCase));
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
    public void TemplatePackCanBeInstalledAndGenerateAMonolithTemplate()
    {
        var templateProjectPath = FindRepositoryFile(Path.Combine("templates", "Cephalon.TemplatePack", "Cephalon.TemplatePack.csproj"));
        var packageOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-pack-{Guid.NewGuid():N}");
        var customHivePath = Path.Combine(Path.GetTempPath(), $"cephalon-template-hive-{Guid.NewGuid():N}");
        var appOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-app-{Guid.NewGuid():N}");
        var moduleOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-module-{Guid.NewGuid():N}");

        Directory.CreateDirectory(packageOutputPath);
        Directory.CreateDirectory(customHivePath);

        try
        {
            var packResult = RunProcess(
                "dotnet",
                $"pack \"{templateProjectPath}\" -c Debug -o \"{packageOutputPath}\"",
                workingDirectory: Path.GetDirectoryName(templateProjectPath)!);

            Assert.Equal(0, packResult.ExitCode);

            var packagePath = Directory.GetFiles(packageOutputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

            var installResult = RunProcess(
                "dotnet",
                $"new install \"{packagePath}\" --debug:custom-hive \"{customHivePath}\"",
                workingDirectory: packageOutputPath);

            Assert.Equal(0, installResult.ExitCode);

            var generateResult = RunProcess(
                "dotnet",
                $"new cephalon-monolith -n Acme.Store -o \"{appOutputPath}\" --debug:custom-hive \"{customHivePath}\"",
                workingDirectory: packageOutputPath);

            Assert.Equal(0, generateResult.ExitCode);

            Assert.True(File.Exists(Path.Combine(appOutputPath, "Acme.Store.csproj")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "appsettings.json")));
            Assert.True(Directory.Exists(Path.Combine(appOutputPath, "Modules", "Catalog")));

            var programPath = Path.Combine(appOutputPath, "Program.cs");
            Assert.Contains("Acme.Store", File.ReadAllText(programPath));
            var appProjectPath = Path.Combine(appOutputPath, "Acme.Store.csproj");
            var appProjectContents = File.ReadAllText(appProjectPath);
            Assert.Contains("Configurations\\**\\*.json", appProjectContents, StringComparison.Ordinal);
            Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", appProjectContents, StringComparison.Ordinal);
            var appSettingsPath = Path.Combine(appOutputPath, "appsettings.json");
            var appSettingsContents = File.ReadAllText(appSettingsPath);
            Assert.Contains("\"ReferenceDocs\"", appSettingsContents, StringComparison.Ordinal);
            Assert.Contains("\"Enabled\": false", appSettingsContents, StringComparison.Ordinal);
            Assert.Contains("\"DirectoryPath\": \"..\\\\..\\\\docs\\\\reference\"", appSettingsContents, StringComparison.Ordinal);

            var appReadmePath = Path.Combine(appOutputPath, "README.md");
            var appReadmeContents = File.ReadAllText(appReadmePath);
            Assert.Contains("ReferenceDocs:Enabled", appReadmeContents, StringComparison.Ordinal);

            var generateModuleResult = RunProcess(
                "dotnet",
                $"new cephalon-rest-module -n OperationsKit -o \"{moduleOutputPath}\" --debug:custom-hive \"{customHivePath}\"",
                workingDirectory: packageOutputPath);

            Assert.Equal(0, generateModuleResult.ExitCode);
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "OperationsKit.csproj")));
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "Registration", "RestModuleEntry.cs")));
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "cephalon.package.json")));

            var moduleEntryPath = Path.Combine(moduleOutputPath, "Registration", "RestModuleEntry.cs");
            var moduleEntryContents = File.ReadAllText(moduleEntryPath);
            Assert.Contains("IRestModule", moduleEntryContents, StringComparison.Ordinal);
            Assert.Contains("MapRestEndpoints", moduleEntryContents, StringComparison.Ordinal);

            var moduleProjectPath = Path.Combine(moduleOutputPath, "OperationsKit.csproj");
            var moduleProjectContents = File.ReadAllText(moduleProjectPath);
            Assert.Contains("<Content Include=\"cephalon.package.json\">", moduleProjectContents, StringComparison.Ordinal);

            var packageManifestPath = Path.Combine(moduleOutputPath, "cephalon.package.json");
            var packageManifestContents = File.ReadAllText(packageManifestPath);
            Assert.Contains("\"version\": \"0.1.0-preview\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"assembly\": \"OperationsKit.dll\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"publisher\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"id\": \"operationskit\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"displayName\": \"OperationsKit\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"minimumEngineVersion\": \"0.1.0-preview\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"supportedTargetFrameworks\": [ \"net10.0\" ]", packageManifestContents, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(moduleOutputPath))
            {
                Directory.Delete(moduleOutputPath, recursive: true);
            }

            if (Directory.Exists(appOutputPath))
            {
                Directory.Delete(appOutputPath, recursive: true);
            }

            if (Directory.Exists(customHivePath))
            {
                Directory.Delete(customHivePath, recursive: true);
            }

            if (Directory.Exists(packageOutputPath))
            {
                Directory.Delete(packageOutputPath, recursive: true);
            }
        }
    }

    private static string FindRepositoryDirectory(string relativePath)
    {
        return RepositoryPaths.GetDirectory(relativePath);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        return RepositoryPaths.GetFile(relativePath);
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
