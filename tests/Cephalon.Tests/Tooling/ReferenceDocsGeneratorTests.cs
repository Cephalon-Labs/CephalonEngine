using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;
using System.Text.Json;

namespace Cephalon.Tests.Tooling;

public sealed class ReferenceDocsGeneratorTests
{
    [Fact]
    public void GenerateBuildsKnownAssemblyPagesFromXmlComments()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Engine", "Cephalon.Agentics"]);

        var rendered = ReferenceDocsGenerator.Generate(request);

        var index = Assert.Single(rendered.Files, file => file.Path == "index.md");
        var readme = Assert.Single(rendered.Files, file => file.Path == "README.md");
        var namespaceIndex = Assert.Single(rendered.Files, file => file.Path == "namespaces.md");
        var typeIndex = Assert.Single(rendered.Files, file => file.Path == "types.md");
        var memberIndex = Assert.Single(rendered.Files, file => file.Path == "members.md");
        var manifest = Assert.Single(rendered.Files, file => file.Path == "reference-manifest.json");
        var browserPage = Assert.Single(rendered.Files, file => file.Path == "browse.html");
        var browserStyles = Assert.Single(rendered.Files, file => file.Path == "reference-browser.css");
        var browserScript = Assert.Single(rendered.Files, file => file.Path == "reference-browser.js");
        var enginePage = Assert.Single(rendered.Files, file => file.Path == "cephalon-engine.md");
        var agenticsPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-agentics.md");

        Assert.Equal(index.Contents, readme.Contents);
        Assert.Contains("[Browser UI](browse.html)", index.Contents, StringComparison.Ordinal);
        Assert.Contains("browse.html?assembly=Cephalon.Engine", index.Contents, StringComparison.Ordinal);
        Assert.Contains("[Namespace index](namespaces.md)", index.Contents, StringComparison.Ordinal);
        Assert.Contains("[Type index](types.md)", index.Contents, StringComparison.Ordinal);
        Assert.Contains("[Member index](members.md)", index.Contents, StringComparison.Ordinal);
        Assert.Contains("### Core", index.Contents, StringComparison.Ordinal);
        Assert.Contains("### Technology Packs", index.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine", index.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Agentics", index.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.Runtime", namespaceIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("cephalon-engine.md#namespace-cephalon-engine-runtime", namespaceIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("browse.html?assembly=Cephalon.Engine&namespace=Cephalon.Engine.Runtime", namespaceIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("EngineBuilder", typeIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("cephalon-engine.md#type-cephalon-engine-composition-enginebuilder", typeIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("browse.html?q=EngineBuilder&assembly=Cephalon.Engine&namespace=Cephalon.Engine.Composition", typeIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("Services", memberIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("cephalon-engine.md#member-p-cephalon-engine-composition-enginebuilder-services", memberIndex.Contents, StringComparison.Ordinal);
        Assert.Contains("scope=members", memberIndex.Contents, StringComparison.Ordinal);
        using (var manifestDocument = JsonDocument.Parse(manifest.Contents))
        {
            Assert.Equal(2, manifestDocument.RootElement.GetProperty("SchemaVersion").GetInt32());
            Assert.Contains(
                manifestDocument.RootElement.GetProperty("Assemblies").EnumerateArray(),
                assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Engine", StringComparison.Ordinal));
            Assert.Contains(
                manifestDocument.RootElement.GetProperty("Types").EnumerateArray(),
                type => string.Equals(type.GetProperty("DisplayName").GetString(), "EngineBuilder", StringComparison.Ordinal));
            Assert.Contains(
                manifestDocument.RootElement.GetProperty("Members").EnumerateArray(),
                member => string.Equals(member.GetProperty("DisplayName").GetString(), "Services", StringComparison.Ordinal) &&
                    string.Equals(member.GetProperty("DeclaringTypeName").GetString(), "EngineBuilder", StringComparison.Ordinal));
        }
        Assert.Contains("reference-browser.css", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains("reference-browser.js", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains("reference-manifest", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains("Reference Browser", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains("scope-filter", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains("namespace-filter", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains("clear-filters", browserPage.Contents, StringComparison.Ordinal);
        Assert.Contains(".hero", browserStyles.Contents, StringComparison.Ordinal);
        Assert.Contains(".action-control", browserStyles.Contents, StringComparison.Ordinal);
        Assert.Contains("renderTypeCard", browserScript.Contents, StringComparison.Ordinal);
        Assert.Contains("renderMemberCard", browserScript.Contents, StringComparison.Ordinal);
        Assert.Contains("applyInitialState", browserScript.Contents, StringComparison.Ordinal);
        Assert.Contains("history.replaceState", browserScript.Contents, StringComparison.Ordinal);
        Assert.Contains("member-count", browserScript.Contents, StringComparison.Ordinal);
        Assert.Contains("EngineBuilder", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("IRuntime", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Engine)", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("<a id=\"namespace-cephalon-engine-runtime\"></a>", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("<a id=\"type-cephalon-engine-composition-enginebuilder\"></a>", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("<a id=\"member-p-cephalon-engine-composition-enginebuilder-services\"></a>", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("AddAgentics", agenticsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IAgentToolCatalog", agenticsPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPagesForPackageBackedHostAssemblies()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-host-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.AspNetCore"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var hostPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-aspnetcore.md");

        Assert.Contains("EngineWebApplicationExtensions", hostPage.Contents, StringComparison.Ordinal);
        Assert.Contains("ITransportRouteMapper", hostPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateIncludesSummariesForCurrentDocumentedPublicAssemblies()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-coverage-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies:
            [
                "Cephalon.Abstractions",
                "Cephalon.Agentics",
                "Cephalon.AspNetCore",
                "Cephalon.AspNetCore.GraphQL",
                "Cephalon.AspNetCore.Grpc",
                "Cephalon.AspNetCore.JsonRpc",
                "Cephalon.Cli",
                "Cephalon.Edge",
                "Cephalon.Engine",
                "Cephalon.Eventing",
                "Cephalon.Observability",
                "Cephalon.Observability.HttpDependencies",
                "Cephalon.Observability.PostgresDependencies",
                "Cephalon.Observability.RabbitMqDependencies",
                "Cephalon.Observability.RedisDependencies",
                "Cephalon.Observability.SqlServerDependencies",
                "Cephalon.Observability.OpenTelemetry",
                "Cephalon.ReferenceDocs",
                "Cephalon.Retrieval",
                "Cephalon.Scaffolding",
                "Cephalon.Worker"
            ]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var manifest = Assert.Single(rendered.Files, file => file.Path == "reference-manifest.json");

        using var manifestDocument = JsonDocument.Parse(manifest.Contents);

        var typeEntries = manifestDocument.RootElement.GetProperty("Types").EnumerateArray().ToArray();
        var memberEntries = manifestDocument.RootElement.GetProperty("Members").EnumerateArray().ToArray();

        Assert.NotEmpty(typeEntries);
        Assert.NotEmpty(memberEntries);

        var missingTypeSummaries = typeEntries
            .Where(static type => !type.TryGetProperty("Summary", out var summary) || string.IsNullOrWhiteSpace(summary.GetString()))
            .Select(static type => $"{type.GetProperty("AssemblyName").GetString()}::{type.GetProperty("NamespaceName").GetString()}.{type.GetProperty("DisplayName").GetString()}")
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var missingMemberSummaries = memberEntries
            .Where(static member => !member.TryGetProperty("Summary", out var summary) || string.IsNullOrWhiteSpace(summary.GetString()))
            .Select(static member => $"{member.GetProperty("AssemblyName").GetString()}::{member.GetProperty("DeclaringTypeName").GetString()}.{member.GetProperty("DisplayName").GetString()} [{member.GetProperty("Category").GetString()}]")
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingTypeSummaries.Length == 0,
            CreateMissingSummaryMessage("public types", missingTypeSummaries));
        Assert.True(
            missingMemberSummaries.Length == 0,
            CreateMissingSummaryMessage("public members", missingMemberSummaries));
    }

    [Fact]
    public void GenerateDefaultCatalogIncludesCurrentShippedHostCompanions()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-default-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration());

        var rendered = ReferenceDocsGenerator.Generate(request);

        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-aspnetcore-graphql.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-postgresdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-rabbitmqdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-redisdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-sqlserverdependencies.md");

        var manifest = Assert.Single(rendered.Files, file => file.Path == "reference-manifest.json");
        using var manifestDocument = JsonDocument.Parse(manifest.Contents);
        var assemblies = manifestDocument.RootElement.GetProperty("Assemblies").EnumerateArray().ToArray();

        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.AspNetCore.GraphQL", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.PostgresDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.RabbitMqDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.RedisDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.SqlServerDependencies", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WriteAsyncWritesRenderedReferenceDocsToDisk()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-write-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Engine"]);
        var rendered = ReferenceDocsGenerator.Generate(request);

        try
        {
            await ReferenceDocsWriter.WriteAsync(rendered, overwrite: true);

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
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
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

    private static string CreateMissingSummaryMessage(string scope, string[] entries)
    {
        const int previewCount = 20;

        var preview = entries
            .Take(previewCount)
            .Select(static entry => $"- {entry}");
        var suffix = entries.Length > previewCount
            ? $"{Environment.NewLine}... and {entries.Length - previewCount} more."
            : string.Empty;

        return $"Reference docs are missing XML summaries for {scope}:{Environment.NewLine}{string.Join(Environment.NewLine, preview)}{suffix}";
    }
}
