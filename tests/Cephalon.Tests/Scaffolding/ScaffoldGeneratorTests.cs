using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Scaffolding.Generation;
using Cephalon.Scaffolding.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Linq;

namespace Cephalon.Tests.Scaffolding;

public sealed class ScaffoldGeneratorTests
{
    [Fact]
    public void GenerateMaterializesProjectsFoldersAndFilesFromAppProfileScaffold()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            transports: ["JsonRpc", "Grpc", "GraphQL"],
            technologies: ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "EdgeNativeDelivery"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Explorer",
                modules: ["Platform", "Discovery"],
                features: ["Greetings"],
                cephalonPackageVersion: "9.1.0-preview"));

        Assert.Contains(scaffold.Projects, project =>
            project.Name == "Acme.Explorer.Host" &&
            project.Packages.Contains("Cephalon.Agentics", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Eventing", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Edge", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Retrieval", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.GraphQL", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.JsonRpc", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.Grpc", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(scaffold.Folders, folder =>
            folder.Path == "src/Acme.Explorer.Modules.Platform/Features/Greetings/Commands");

        var solution = Assert.Single(scaffold.Files, file => file.Path == "Acme.Explorer.slnx");
        Assert.Contains("src/Acme.Explorer.Host/Acme.Explorer.Host.csproj", solution.Contents, StringComparison.Ordinal);

        var hostSettings = Assert.Single(scaffold.Files, file => file.Path == "src/Acme.Explorer.Host/appsettings.json");
        Assert.Contains("Acme.Explorer.Modules.Platform", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"JSON-RPC\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"gRPC\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"GraphQL\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Technologies\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Agentic Workloads\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Localization\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"engine.docs.rest.title\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Telemetry\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"http://localhost:4317\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"ReferenceDocs\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"Enabled\": false", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"DirectoryPath\": \"..\\\\..\\\\docs\\\\reference\"", hostSettings.Contents, StringComparison.Ordinal);
        Assert.Contains("\"DefaultDocument\": \"browse.html\"", hostSettings.Contents, StringComparison.Ordinal);

        var packageProps = Assert.Single(scaffold.Files, file => file.Path == "Directory.Packages.props");
        Assert.Contains("Cephalon.Agentics", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Eventing", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Edge", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Retrieval", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.GraphQL", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.JsonRpc", packageProps.Contents, StringComparison.Ordinal);
        Assert.Contains("Version=\"9.1.0-preview\"", packageProps.Contents, StringComparison.Ordinal);

        var directoryBuildProps = Assert.Single(scaffold.Files, file => file.Path == "Directory.Build.props");
        Assert.Contains("<GenerateDocumentationFile>true</GenerateDocumentationFile>", directoryBuildProps.Contents, StringComparison.Ordinal);

        var moduleProjectFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Modules.Platform/Acme.Explorer.Modules.Platform.csproj");
        Assert.Contains("<Content Include=\"cephalon.package.json\">", moduleProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", moduleProjectFile.Contents, StringComparison.Ordinal);

        var moduleManifestFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Modules.Platform/cephalon.package.json");
        Assert.Contains("\"id\": \"platform\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"version\": \"9.1.0-preview\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"assembly\": \"Acme.Explorer.Modules.Platform.dll\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"publisher\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"id\": \"acme.explorer\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"displayName\": \"Acme.Explorer\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"minimumEngineVersion\": \"9.1.0-preview\"", moduleManifestFile.Contents, StringComparison.Ordinal);
        Assert.Contains("\"supportedTargetFrameworks\": [ \"net10.0\" ]", moduleManifestFile.Contents, StringComparison.Ordinal);

        var hostProjectFile = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Acme.Explorer.Host.csproj");
        Assert.Contains("Configurations\\**\\*.json", hostProjectFile.Contents, StringComparison.Ordinal);
        Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", hostProjectFile.Contents, StringComparison.Ordinal);

        var hostProgram = Assert.Single(
            scaffold.Files,
            file => file.Path == "src/Acme.Explorer.Host/Program.cs");
        Assert.Contains("builder.AddGraphQLTransport();", hostProgram.Contents, StringComparison.Ordinal);

        var readme = Assert.Single(scaffold.Files, file => file.Path == "README.md");
        Assert.Contains("Engine:Localization", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Agentic Workloads", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Event-Driven Integration", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Edge-Native Delivery", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("ReferenceDocs", readme.Contents, StringComparison.Ordinal);
        Assert.Contains("Configurations/[group]/[Environment].json", readme.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateKeepsPackageVersionsAlignedWithRepositoryCatalog()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var scaffold = ScaffoldGenerator.Generate(
            runtime.Manifest.AppProfile,
            new ScaffoldRequest(
                appName: "Acme.Catalog",
                modules: ["Platform"],
                cephalonPackageVersion: "2.0.0-preview"));

        var generatedPackages = ParsePackageVersions(
            Assert.Single(scaffold.Files, file => file.Path == "Directory.Packages.props").Contents);
        var repositoryPackages = ParsePackageVersions(File.ReadAllText(FindRepositoryFile("Directory.Packages.props")));

        foreach (var packageId in new[]
                 {
                     "coverlet.collector",
                     "Microsoft.NET.Test.Sdk",
                     "xunit",
                     "xunit.runner.visualstudio"
                 })
        {
            Assert.Equal(repositoryPackages[packageId], generatedPackages[packageId]);
        }
    }

    [Fact]
    public async Task WriteAsyncWritesScaffoldToDiskAndPreventsAccidentalOverwrite()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));

        var runtime = builder.Build();
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-scaffold-{Guid.NewGuid():N}");

        try
        {
            var scaffold = ScaffoldGenerator.Generate(
                runtime.Manifest.AppProfile,
                new ScaffoldRequest(
                    appName: "Future.Stack",
                    modules: ["Platform"],
                    cephalonPackageVersion: "1.0.0-preview"));

            await FileSystemScaffoldWriter.WriteAsync(outputPath, scaffold);

            Assert.True(File.Exists(Path.Combine(outputPath, "Future.Stack.slnx")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Future.Stack.Host", "Program.cs")));
            Assert.True(Directory.Exists(Path.Combine(outputPath, "src", "Future.Stack.Modules.Platform", "Application")));
            Assert.True(File.Exists(Path.Combine(outputPath, "src", "Future.Stack.Modules.Platform", "cephalon.package.json")));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => FileSystemScaffoldWriter.WriteAsync(outputPath, scaffold));
            Assert.Contains("already exists", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    private static Dictionary<string, string> ParsePackageVersions(string xml)
    {
        var document = XDocument.Parse(xml);

        return document
            .Descendants("PackageVersion")
            .ToDictionary(
                element => element.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException("PackageVersion Include attribute was missing."),
                element => element.Attribute("Version")?.Value
                    ?? throw new InvalidOperationException("PackageVersion Version attribute was missing."),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidatePath = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find '{fileName}' from '{AppContext.BaseDirectory}'.");
    }
}
