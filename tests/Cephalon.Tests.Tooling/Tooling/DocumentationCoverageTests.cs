using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cephalon.Tests.Tooling;

public sealed class DocumentationCoverageTests
{
    private static readonly Regex MarkdownLinkPattern = new(
        @"\[[^\]]+\]\((?<target>[^)]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FencedCodeBlockPattern = new(
        @"(^|\r?\n)```[\s\S]*?(\r?\n```|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MarkdownHeadingPattern = new(
        @"^\s{0,3}#{1,6}\s+(?<text>.+?)(?:\s+#+\s*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex MarkdownInlineLinkTextPattern = new(
        @"!?\[(?<text>[^\]]+)\]\([^)]+\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlTagPattern = new(
        @"<[^>]+>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlAnchorPattern = new(
        @"<a\s+[^>]*(?:id|name)\s*=\s*[""'](?<anchor>[^""']+)[""'][^>]*>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex HtmlReferenceAttributePattern = new(
        @"\b(?:href|src)\s*=\s*[""'](?<target>[^""']+)[""']",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex GitHubLineFragmentPattern = new(
        @"^L(?<start>\d+)(?:-L(?<end>\d+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // Projects that are internal implementation helpers (IsPackable=false) — not shipped as NuGet packages
    // and therefore do not require component documentation.
    private static readonly HashSet<string> NonPackableProjects = new(StringComparer.Ordinal);

    // Projects whose doc filename does not follow the default convention
    // (strip "Cephalon." prefix, replace "." with "-", lowercase, append ".md").
    // Keep this list as short as possible — only add entries when the convention
    // produces the wrong filename.
    private static readonly Dictionary<string, string> SlugOverrides =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // "EventSourcing" is one dot-segment but the slug splits the word
            ["Cephalon.EventSourcing"] = "event-sourcing.md",
            ["Cephalon.EventSourcing.Cassandra"] = "event-sourcing-cassandra.md",
            ["Cephalon.EventSourcing.ClickHouse"] = "event-sourcing-clickhouse.md",
            ["Cephalon.EventSourcing.EntityFramework"] = "event-sourcing-entityframework.md",
            ["Cephalon.EventSourcing.MongoDB"] = "event-sourcing-mongodb.md",
            ["Cephalon.EventSourcing.Neo4j"] = "event-sourcing-neo4j.md",
            ["Cephalon.EventSourcing.Elasticsearch"] = "event-sourcing-elasticsearch.md",
            ["Cephalon.EventSourcing.OpenSearch"] = "event-sourcing-opensearch.md",
            ["Cephalon.EventSourcing.Nats"] = "event-sourcing-nats.md",
            ["Cephalon.EventSourcing.Qdrant"] = "event-sourcing-qdrant.md",
            ["Cephalon.EventSourcing.Redis"] = "event-sourcing-redis.md",
            // "MultiTenancy" → "multi-tenancy"
            ["Cephalon.MultiTenancy"] = "multi-tenancy.md",
            ["Cephalon.MultiTenancy.Governance"] = "multi-tenancy-governance.md",
            ["Cephalon.MultiTenancy.Governance.AspNetCore"] = "multi-tenancy-governance-aspnetcore.md",
            ["Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore"] =
                "multi-tenancy-governance-amazonsesdelivery-aspnetcore.md",
            ["Cephalon.MultiTenancy.Governance.AmazonSesDelivery"] = "multi-tenancy-governance-amazonsesdelivery.md",
            ["Cephalon.MultiTenancy.Governance.HttpDelivery"] = "multi-tenancy-governance-httpdelivery.md",
            ["Cephalon.MultiTenancy.Governance.MailgunDelivery"] = "multi-tenancy-governance-mailgundelivery.md",
            ["Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore"] =
                "multi-tenancy-governance-mailgundelivery-aspnetcore.md",
            ["Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery"] =
                "multi-tenancy-governance-microsoftgraphdelivery.md",
            ["Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity"] =
                "multi-tenancy-governance-microsoftgraphdelivery-azureidentity.md",
            ["Cephalon.MultiTenancy.Governance.SendGridDelivery"] = "multi-tenancy-governance-sendgriddelivery.md",
            ["Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore"] =
                "multi-tenancy-governance-sendgriddelivery-aspnetcore.md",
            ["Cephalon.MultiTenancy.Governance.SmtpDelivery"] = "multi-tenancy-governance-smtpdelivery.md",
            // "ReferenceDocs" → "reference-docs"
            ["Cephalon.ReferenceDocs"] = "reference-docs.md",
            // "KubernetesGateway" → "kubernetes-gateway"
            ["Cephalon.Edge.KubernetesGateway"] = "edge-kubernetes-gateway.md",
            // Observability slugs that split compound words with a hyphen
            ["Cephalon.Observability.AlibabaCloud"] = "observability-alibaba-cloud.md",
            ["Cephalon.Observability.AzureMonitor"] = "observability-azure-monitor.md",
            ["Cephalon.Observability.CassandraDependencies"] = "observability-cassandra-dependencies.md",
            ["Cephalon.Observability.ClickHouseDependencies"] = "observability-clickhouse-dependencies.md",
            ["Cephalon.Observability.ConsulDependencies"] = "observability-consul-dependencies.md",
            ["Cephalon.Observability.DependencyHealth.Core"] = "observability-dependency-health-core.md",
            ["Cephalon.Observability.ElasticsearchDependencies"] = "observability-elasticsearch-dependencies.md",
            ["Cephalon.Observability.GrafanaCloud"] = "observability-grafana-cloud.md",
            ["Cephalon.Observability.HttpDependencies"] = "observability-http-dependencies.md",
            ["Cephalon.Observability.HuaweiCloud"] = "observability-huawei-cloud.md",
            ["Cephalon.Observability.KafkaDependencies"] = "observability-kafka-dependencies.md",
            ["Cephalon.Observability.MemcachedDependencies"] = "observability-memcached-dependencies.md",
            ["Cephalon.Observability.MongoDbDependencies"] = "observability-mongodb-dependencies.md",
            ["Cephalon.Observability.MqttDependencies"] = "observability-mqtt-dependencies.md",
            ["Cephalon.Observability.MySqlDependencies"] = "observability-mysql-dependencies.md",
            ["Cephalon.Observability.NatsDependencies"] = "observability-nats-dependencies.md",
            ["Cephalon.Observability.Neo4jDependencies"] = "observability-neo4j-dependencies.md",
            ["Cephalon.Observability.NewRelic"] = "observability-new-relic.md",
            ["Cephalon.Observability.OpenSearchDependencies"] = "observability-opensearch-dependencies.md",
            ["Cephalon.Observability.OracleCloud"] = "observability-oracle-cloud.md",
            ["Cephalon.Observability.OracleDependencies"] = "observability-oracle-dependencies.md",
            ["Cephalon.Observability.PostgresDependencies"] = "observability-postgres-dependencies.md",
            ["Cephalon.Observability.RabbitMqDependencies"] = "observability-rabbitmq-dependencies.md",
            ["Cephalon.Observability.RedisDependencies"] = "observability-redis-dependencies.md",
            ["Cephalon.Observability.SqlServerDependencies"] = "observability-sqlserver-dependencies.md",
            // "SciSharpReplication" -> "scisharp-replication"
            ["Cephalon.Data.MySql.SciSharpReplication"] = "data-mysql-scisharp-replication.md",
        };

    private static string DeriveComponentDocFileName(string projectName)
    {
        if (SlugOverrides.TryGetValue(projectName, out var overrideSlug))
            return overrideSlug;

        // Convention: strip "Cephalon." prefix, replace "." with "-", lowercase, append ".md"
        var withoutPrefix = projectName.StartsWith("Cephalon.", StringComparison.Ordinal)
            ? projectName["Cephalon.".Length..]
            : projectName;

        return withoutPrefix.Replace('.', '-').ToLowerInvariant() + ".md";
    }

    [Fact]
    public void EveryShippedSourceProjectHasAComponentDocumentAndCatalogEntry()
    {
        var repositoryRoot = GetRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");
        var componentCatalog = File.ReadAllText(Path.Combine(componentDocsRoot, "README.md"));

        var projectDirectories = Directory
            .GetDirectories(sourceRoot, "Cephalon.*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !NonPackableProjects.Contains(name!))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var projectName in projectDirectories)
        {
            var componentDocFileName = DeriveComponentDocFileName(projectName!);

            var componentDocPath = Path.Combine(componentDocsRoot, componentDocFileName);
            Assert.True(
                File.Exists(componentDocPath),
                $"Expected a component doc for '{projectName}' at '{componentDocPath}'. " +
                $"If the filename does not follow convention, add an override in {nameof(SlugOverrides)}.");

            var componentDocContents = File.ReadAllText(componentDocPath);
            Assert.Contains(projectName!, componentDocContents, StringComparison.Ordinal);
            Assert.Contains("> **Maturity:** `", componentDocContents, StringComparison.Ordinal);
            Assert.Contains("**Ownership:**", componentDocContents, StringComparison.Ordinal);
            Assert.Contains(
                "authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)",
                componentDocContents,
                StringComparison.Ordinal);
            var componentCatalogEntry = $"[{projectName}]({componentDocFileName})";
            var componentCatalogEntryCount = CountOrdinalOccurrences(componentCatalog, componentCatalogEntry);
            Assert.True(
                componentCatalogEntryCount == 1,
                $"Expected component catalog entry '{componentCatalogEntry}' to appear exactly once but found {componentCatalogEntryCount}.");
        }
    }

    [Fact]
    public void ComponentMaturityBadgesAndAuditBaselineStayCurrent()
    {
        var repositoryRoot = GetRepositoryRoot();
        var componentCatalog = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "README.md"));
        var maturityAudit = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-surface-maturity-audit.md"));
        var backlog = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-backlog.md"));

        Assert.Equal(
            ReadDocumentBaselineDate(backlog, "Backlog status in this document reflects the repository state as of"),
            ReadDocumentBaselineDate(maturityAudit, "Surface maturity in this document reflects the repository state as of"));
        Assert.Contains("Every shipped source-project component page now carries this badge.", componentCatalog, StringComparison.Ordinal);
        Assert.DoesNotContain("being rolled out incrementally", componentCatalog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tooling coverage fails if a shipped `src/Cephalon.*` project has a component page without a maturity label", componentCatalog, StringComparison.Ordinal);
        Assert.Contains("ProviderIntegrationEvidence.DependencyHealthProviderManifest", componentCatalog, StringComparison.Ordinal);
        Assert.Contains("scorecard schema `1.25.0`", componentCatalog, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard", componentCatalog, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryComponentDocumentIsCatalogedExactlyOnce()
    {
        var repositoryRoot = GetRepositoryRoot();
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");
        var componentCatalog = File.ReadAllText(Path.Combine(componentDocsRoot, "README.md"));
        var componentDocFileNames = Directory
            .EnumerateFiles(componentDocsRoot, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static fileName => !string.Equals(fileName, "README.md", StringComparison.Ordinal))
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(componentDocFileNames);

        foreach (var componentDocFileName in componentDocFileNames)
        {
            var componentCatalogTargetCount = CountOrdinalOccurrences(componentCatalog, $"]({componentDocFileName})");
            Assert.True(
                componentCatalogTargetCount == 1,
                $"Expected component document '{componentDocFileName}' to be linked exactly once from docs/components/README.md but found {componentCatalogTargetCount}.");
        }
    }

    [Fact]
    public void ComponentCatalogLocalLinksResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var componentCatalogPath = Path.Combine(repositoryRoot, "docs", "components", "README.md");

        AssertLocalMarkdownLinksResolve(repositoryRoot, componentCatalogPath, "component catalog");
    }

    [Fact]
    public void ComponentDocumentLocalLinksResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");
        var componentDocPaths = Directory
            .EnumerateFiles(componentDocsRoot, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(componentDocPaths);

        foreach (var componentDocPath in componentDocPaths)
            AssertLocalMarkdownLinksResolve(repositoryRoot, componentDocPath, $"component document '{Path.GetFileName(componentDocPath)}'");
    }

    [Fact]
    public void TopLevelDocumentationLocalLinksResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var docsRoot = Path.Combine(repositoryRoot, "docs");
        var topLevelDocPaths = Directory
            .EnumerateFiles(docsRoot, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(topLevelDocPaths);

        var totalLocalLinkCount = 0;
        foreach (var topLevelDocPath in topLevelDocPaths)
            totalLocalLinkCount += AssertLocalMarkdownLinksResolve(
                repositoryRoot,
                topLevelDocPath,
                $"top-level documentation '{Path.GetFileName(topLevelDocPath)}'",
                allowDirectoryTargets: true,
                requireLocalLinks: false);

        Assert.True(totalLocalLinkCount > 0, "Expected top-level documentation to contain at least one repo-local Markdown link.");
    }

    [Fact]
    public void HandAuthoredMarkdownLocalLinksResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var markdownPaths = EnumerateHandAuthoredMarkdownPaths(repositoryRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(markdownPaths);

        var totalLocalLinkCount = 0;
        foreach (var markdownPath in markdownPaths)
            totalLocalLinkCount += AssertLocalMarkdownLinksResolve(
                repositoryRoot,
                markdownPath,
                $"hand-authored Markdown '{Path.GetRelativePath(repositoryRoot, markdownPath)}'",
                allowDirectoryTargets: true,
                requireLocalLinks: false);

        Assert.True(totalLocalLinkCount > 0, "Expected hand-authored Markdown to contain at least one repo-local Markdown link.");
    }

    [Fact]
    public void GeneratedReferenceDocumentationLocalLinksResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var referenceDocsRoot = Path.Combine(repositoryRoot, "docs", "reference");
        var markdownPaths = EnumerateReferenceMarkdownPaths(referenceDocsRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(markdownPaths);

        var totalLocalLinkCount = 0;
        foreach (var markdownPath in markdownPaths)
            totalLocalLinkCount += AssertLocalMarkdownLinksResolve(
                repositoryRoot,
                markdownPath,
                $"generated reference documentation '{Path.GetRelativePath(referenceDocsRoot, markdownPath)}'",
                requireLocalLinks: false,
                requiredContainingDirectory: referenceDocsRoot);

        Assert.True(totalLocalLinkCount > 0, "Expected generated reference documentation to contain at least one local navigation link.");
    }

    [Fact]
    public void GeneratedReferenceDocumentationManifestTargetsResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var referenceDocsRoot = Path.Combine(repositoryRoot, "docs", "reference");
        var manifestPath = Path.Combine(referenceDocsRoot, "reference-manifest.json");

        Assert.True(File.Exists(manifestPath), "Expected generated reference documentation to include reference-manifest.json.");

        using var manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifest = manifestDocument.RootElement;
        Assert.Equal(2, ReadRequiredJsonInt32(manifest, "SchemaVersion", "reference manifest"));
        Assert.True(
            DateTimeOffset.TryParse(
                ReadRequiredJsonString(manifest, "GeneratedAtUtc", "reference manifest"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out _),
            "Expected reference manifest GeneratedAtUtc to be a valid timestamp.");

        var requiredBundleFiles = new[]
        {
            "README.md",
            "index.md",
            "namespaces.md",
            "types.md",
            "members.md",
            "browse.html",
            "reference-browser.css",
            "reference-browser.js",
            "reference-manifest.json"
        };

        foreach (var requiredBundleFile in requiredBundleFiles)
            AssertReferenceBundleFileExists(referenceDocsRoot, requiredBundleFile, "required generated reference bundle file");

        var assemblies = manifest.GetProperty("Assemblies").EnumerateArray().ToArray();
        Assert.NotEmpty(assemblies);

        var assemblyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assemblyNamespaceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var assemblyTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            var assemblyName = ReadRequiredJsonString(assembly, "AssemblyName", "reference manifest assembly");
            var fileName = ReadRequiredJsonString(assembly, "FileName", $"reference manifest assembly '{assemblyName}'");

            Assert.True(
                assemblyFiles.Add(fileName),
                $"Expected reference manifest assembly file '{fileName}' to be listed once.");
            AssertReferenceBundleFileExists(referenceDocsRoot, fileName, $"reference manifest assembly '{assemblyName}'");
            assemblyNamespaceCounts[assemblyName] = ReadRequiredJsonInt32(assembly, "NamespaceCount", $"reference manifest assembly '{assemblyName}'");
            assemblyTypeCounts[assemblyName] = ReadRequiredJsonInt32(assembly, "TypeCount", $"reference manifest assembly '{assemblyName}'");
        }

        var namespaceEntries = manifest.GetProperty("Namespaces").EnumerateArray().ToArray();
        var typeEntries = manifest.GetProperty("Types").EnumerateArray().ToArray();
        var memberEntries = manifest.GetProperty("Members").EnumerateArray().ToArray();

        Assert.NotEmpty(namespaceEntries);
        Assert.NotEmpty(typeEntries);
        Assert.NotEmpty(memberEntries);

        AssertManifestEntryCountsMatchAssemblies("namespace", namespaceEntries, assemblyNamespaceCounts);
        AssertManifestEntryCountsMatchAssemblies("type", typeEntries, assemblyTypeCounts);

        var anchorCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        AssertReferenceManifestAnchorsResolve(referenceDocsRoot, namespaceEntries, assemblyFiles, anchorCache, "namespace");
        AssertReferenceManifestAnchorsResolve(referenceDocsRoot, typeEntries, assemblyFiles, anchorCache, "type");
        AssertReferenceManifestAnchorsResolve(referenceDocsRoot, memberEntries, assemblyFiles, anchorCache, "member");

        var expectedMarkdownFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "README.md",
            "index.md",
            "namespaces.md",
            "types.md",
            "members.md"
        };

        foreach (var assemblyFile in assemblyFiles)
            expectedMarkdownFiles.Add(assemblyFile);

        var actualMarkdownFiles = EnumerateReferenceMarkdownPaths(referenceDocsRoot)
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToArray();

        Assert.Equal(
            expectedMarkdownFiles.OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase),
            actualMarkdownFiles.OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeneratedReferenceDocumentationBrowserLocalAssetsResolve()
    {
        var repositoryRoot = GetRepositoryRoot();
        var referenceDocsRoot = Path.Combine(repositoryRoot, "docs", "reference");
        var browserPath = Path.Combine(referenceDocsRoot, "browse.html");

        Assert.True(File.Exists(browserPath), "Expected generated reference documentation to include browse.html.");

        var browserHtml = File.ReadAllText(browserPath);
        var localReferences = HtmlReferenceAttributePattern
            .Matches(browserHtml)
            .Select(static match => match.Groups["target"].Value.Trim())
            .Where(IsRepositoryLocalBrowserReference)
            .Select(ParseLocalMarkdownLink)
            .GroupBy(static link => link.OriginalTarget, StringComparer.Ordinal)
            .Select(static group => group.First())
            .OrderBy(static link => link.OriginalTarget, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(localReferences);

        var localReferenceTargets = localReferences
            .Select(static link => link.TargetPath)
            .ToArray();
        Assert.Contains("README.md", localReferenceTargets);
        Assert.Contains("reference-manifest.json", localReferenceTargets);
        Assert.Contains("reference-browser.css", localReferenceTargets);
        Assert.Contains("reference-browser.js", localReferenceTargets);

        foreach (var localReference in localReferences)
        {
            var resolvedPath = ResolveLocalMarkdownLinkPath(referenceDocsRoot, browserPath, localReference.TargetPath);

            Assert.True(
                IsPathInsideDirectory(referenceDocsRoot, resolvedPath),
                $"Expected generated reference browser target '{localReference.OriginalTarget}' to stay inside docs/reference but resolved to '{resolvedPath}'.");
            Assert.True(
                File.Exists(resolvedPath),
                $"Expected generated reference browser target '{localReference.OriginalTarget}' to resolve to an existing generated reference-doc file at '{resolvedPath}'.");
        }
    }

    [Fact]
    public void ObservabilityDependencyProbeDocsMatchRuntimeOwnership()
    {
        var repositoryRoot = GetRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");
        var componentsReadme = File.ReadAllText(Path.Combine(componentDocsRoot, "README.md"));
        var providers = ReadDependencyHealthProviderManifest(repositoryRoot);

        var providerProjects = Directory
            .GetDirectories(sourceRoot, "Cephalon.Observability.*Dependencies", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.Equals(name, "Cephalon.Observability.DependencyHealth.Core", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(18, providers.Length);
        Assert.Equal(
            providers.Select(static provider => provider.Source).OrderBy(source => source, StringComparer.Ordinal),
            providerProjects);

        foreach (var provider in providers)
        {
            var projectRoot = Path.Combine(sourceRoot, provider.Source);
            Assert.True(
                File.Exists(Path.Combine(projectRoot, $"{provider.Source}.csproj")),
                $"Expected provider project '{provider.Source}' to exist.");

            var componentDocPath = Path.Combine(repositoryRoot, NormalizeRepositoryPath(provider.ComponentDoc));
            var hostedServicePath = Path.Combine(repositoryRoot, NormalizeRepositoryPath(provider.HostedServiceFile));
            var diagnosticsContributorPath = Path.Combine(repositoryRoot, NormalizeRepositoryPath(provider.DiagnosticsContributorFile));
            var configurationOptionsPath = Path.Combine(repositoryRoot, NormalizeRepositoryPath(provider.ConfigurationOptionsFile));
            var hostingExtensionPath = Path.Combine(repositoryRoot, NormalizeRepositoryPath(provider.HostingExtensionFile));

            Assert.True(File.Exists(componentDocPath), $"Missing component doc '{provider.ComponentDoc}'.");
            Assert.True(File.Exists(hostedServicePath), $"Missing hosted service '{provider.HostedServiceFile}'.");
            Assert.True(File.Exists(diagnosticsContributorPath), $"Missing diagnostics contributor '{provider.DiagnosticsContributorFile}'.");
            Assert.True(File.Exists(configurationOptionsPath), $"Missing configuration options '{provider.ConfigurationOptionsFile}'.");
            Assert.True(File.Exists(hostingExtensionPath), $"Missing hosting extension '{provider.HostingExtensionFile}'.");

            var componentDoc = File.ReadAllText(componentDocPath);
            Assert.Equal(DeriveComponentDocFileName(provider.Source), Path.GetFileName(provider.ComponentDoc));
            Assert.Contains($"[{provider.Source}]({Path.GetFileName(provider.ComponentDoc)})", componentsReadme, StringComparison.Ordinal);
            Assert.Contains($"**Maturity:** `{provider.Maturity}`", componentDoc, StringComparison.Ordinal);
            Assert.Contains($"**Ownership:** `{provider.Ownership}`", componentDoc, StringComparison.Ordinal);
            Assert.DoesNotContain("**Maturity:** `M0`", componentDoc, StringComparison.Ordinal);
            Assert.DoesNotContain("**Ownership:** `taxonomy-only`", componentDoc, StringComparison.Ordinal);

            var optionsSource = File.ReadAllText(configurationOptionsPath);
            Assert.Contains(provider.OptionsType, optionsSource, StringComparison.Ordinal);
            Assert.Contains(".GetSection(\"DependencyHealth\")", optionsSource, StringComparison.Ordinal);
            Assert.Contains($".GetSection(\"{provider.ConfigurationSectionName}\")", optionsSource, StringComparison.Ordinal);

            var hostingSource = File.ReadAllText(hostingExtensionPath);
            Assert.Contains(provider.ExtensionMethod, hostingSource, StringComparison.Ordinal);
            Assert.Contains(provider.DefinitionType, hostingSource, StringComparison.Ordinal);
            Assert.Contains(provider.OptionsType, hostingSource, StringComparison.Ordinal);

            var hostedServiceSource = File.ReadAllText(hostedServicePath);
            Assert.Contains(provider.DefinitionType, hostedServiceSource, StringComparison.Ordinal);

            var diagnosticsContributorSource = File.ReadAllText(diagnosticsContributorPath);
            Assert.Contains($"Source: \"{provider.Source}\"", diagnosticsContributorSource, StringComparison.Ordinal);
            Assert.Contains("ProbeTimedOut", diagnosticsContributorSource, StringComparison.Ordinal);
            Assert.Contains("ProbeFailed", diagnosticsContributorSource, StringComparison.Ordinal);
        }

        var maturityAudit = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-surface-maturity-audit.md"));
        var conformanceMatrix = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "conformance-matrix.md"));
        var scorecard = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-completion-scorecard.md"));
        var backlog = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-backlog.md"));
        var projectMemory = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "project-memory.md"));

        Assert.Contains("the eighteen per-provider dependency-health probe packs at `M2`", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("`Cephalon.Observability.*Dependencies`", conformanceMatrix, StringComparison.Ordinal);
        Assert.Contains("| M2 | provider-managed | `/engine/dependencies`", conformanceMatrix, StringComparison.Ordinal);
        Assert.Contains("observability-dependency-health-providers.json", componentsReadme, StringComparison.Ordinal);
        Assert.Contains("observability-dependency-health-providers.json", scorecard, StringComparison.Ordinal);
        Assert.Contains("observability-dependency-health-providers.json", backlog, StringComparison.Ordinal);
        Assert.Contains("observability-dependency-health-providers.json", projectMemory, StringComparison.Ordinal);
    }

    [Fact]
    public void MultiTenancyInvitationDeliveryProviderDocsMatchRuntimeSurfaces()
    {
        var repositoryRoot = GetRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");

        var providers = new Dictionary<string, (string Doc, string SurfaceId, string Contributor)>(StringComparer.Ordinal)
        {
            ["Cephalon.MultiTenancy.Governance.HttpDelivery"] = (
                "multi-tenancy-governance-httpdelivery.md",
                "tenant-invitation-delivery-http",
                "HttpInvitationDeliveryRuntimeSurfaceContributor.cs"),
            ["Cephalon.MultiTenancy.Governance.SmtpDelivery"] = (
                "multi-tenancy-governance-smtpdelivery.md",
                "tenant-invitation-delivery-smtp",
                "SmtpInvitationDeliveryRuntimeSurfaceContributor.cs"),
            ["Cephalon.MultiTenancy.Governance.SendGridDelivery"] = (
                "multi-tenancy-governance-sendgriddelivery.md",
                "tenant-invitation-delivery-sendgrid",
                "SendGridInvitationDeliveryRuntimeSurfaceContributor.cs"),
            ["Cephalon.MultiTenancy.Governance.MailgunDelivery"] = (
                "multi-tenancy-governance-mailgundelivery.md",
                "tenant-invitation-delivery-mailgun",
                "MailgunInvitationDeliveryRuntimeSurfaceContributor.cs"),
            ["Cephalon.MultiTenancy.Governance.AmazonSesDelivery"] = (
                "multi-tenancy-governance-amazonsesdelivery.md",
                "tenant-invitation-delivery-amazon-ses",
                "AmazonSesInvitationDeliveryRuntimeSurfaceContributor.cs"),
            ["Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery"] = (
                "multi-tenancy-governance-microsoftgraphdelivery.md",
                "tenant-invitation-delivery-microsoft-graph",
                "MicrosoftGraphInvitationDeliveryRuntimeSurfaceContributor.cs"),
            ["Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity"] = (
                "multi-tenancy-governance-microsoftgraphdelivery-azureidentity.md",
                "tenant-invitation-delivery-microsoft-graph-azure-identity",
                "MicrosoftGraphInvitationDeliveryAzureIdentityRuntimeSurfaceContributor.cs"),
        };

        foreach (var (projectName, expected) in providers)
        {
            var contributorPath = Path.Combine(sourceRoot, projectName, "Services", expected.Contributor);
            Assert.True(File.Exists(contributorPath), $"Expected runtime surface contributor at '{contributorPath}'.");

            var componentDoc = File.ReadAllText(Path.Combine(componentDocsRoot, expected.Doc));
            Assert.Contains("**Maturity:** `M2`", componentDoc, StringComparison.Ordinal);
            Assert.Contains("**Ownership:** `provider-managed`", componentDoc, StringComparison.Ordinal);
            Assert.Contains(expected.SurfaceId, componentDoc, StringComparison.Ordinal);
            Assert.Contains(expected.Contributor, componentDoc, StringComparison.Ordinal);
        }

        var maturityAudit = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-surface-maturity-audit.md"));
        var conformanceMatrix = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "conformance-matrix.md"));

        Assert.Contains("seven outbound sender/token-provider packs now have sanitized provider runtime surfaces", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("seven independent outbound sender/token-provider integrations", conformanceMatrix, StringComparison.Ordinal);

        foreach (var surfaceId in providers.Values.Select(provider => provider.SurfaceId))
        {
            Assert.Contains(surfaceId, maturityAudit, StringComparison.Ordinal);
            Assert.Contains(surfaceId, conformanceMatrix, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EventSourcingProviderDocsMatchRuntimeCatalogContributors()
    {
        var repositoryRoot = GetRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");

        var providers = new Dictionary<string, (string Doc, string Contributor, string Maturity)>(StringComparer.Ordinal)
        {
            ["Cephalon.EventSourcing.Cassandra"] = (
                "event-sourcing-cassandra.md",
                "CassandraEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.ClickHouse"] = (
                "event-sourcing-clickhouse.md",
                "ClickHouseEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.Elasticsearch"] = (
                "event-sourcing-elasticsearch.md",
                "ElasticsearchEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.EntityFramework"] = (
                "event-sourcing-entityframework.md",
                "EntityFrameworkEventStoreContributor.cs",
                "M2"),
            ["Cephalon.EventSourcing.MongoDB"] = (
                "event-sourcing-mongodb.md",
                "MongoDbEventStoreContributor.cs",
                "M2"),
            ["Cephalon.EventSourcing.Nats"] = (
                "event-sourcing-nats.md",
                "NatsEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.Neo4j"] = (
                "event-sourcing-neo4j.md",
                "Neo4jEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.OpenSearch"] = (
                "event-sourcing-opensearch.md",
                "OpenSearchEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.Qdrant"] = (
                "event-sourcing-qdrant.md",
                "QdrantEventStoreContributor.cs",
                "M1"),
            ["Cephalon.EventSourcing.Redis"] = (
                "event-sourcing-redis.md",
                "RedisEventStoreContributor.cs",
                "M2"),
        };

        foreach (var (projectName, expected) in providers)
        {
            var contributorPath = Path.Combine(sourceRoot, projectName, "Services", expected.Contributor);
            Assert.True(File.Exists(contributorPath), $"Expected event-store contributor at '{contributorPath}'.");

            var componentDoc = File.ReadAllText(Path.Combine(componentDocsRoot, expected.Doc));
            Assert.Contains($"**Maturity:** `{expected.Maturity}`", componentDoc, StringComparison.Ordinal);
            Assert.Contains("**Ownership:** `provider-managed`", componentDoc, StringComparison.Ordinal);
            Assert.Contains(expected.Contributor, componentDoc, StringComparison.Ordinal);
            Assert.Contains("`event-sourcing` runtime surface", componentDoc, StringComparison.Ordinal);

            if (projectName is "Cephalon.EventSourcing.EntityFramework" or "Cephalon.EventSourcing.MongoDB" or "Cephalon.EventSourcing.Redis")
            {
                Assert.Contains("snapshot", componentDoc, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("snapshotLifecycle = provider-durable", componentDoc, StringComparison.Ordinal);
            }
        }

        var coreDoc = File.ReadAllText(Path.Combine(componentDocsRoot, "event-sourcing.md"));
        Assert.Contains("one sanitized runtime entry per contributed provider store", coreDoc, StringComparison.Ordinal);
        Assert.Contains("Runtime/EventSourcingRuntimeContributor.cs", coreDoc, StringComparison.Ordinal);

        var maturityAudit = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-surface-maturity-audit.md"));
        var conformanceMatrix = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "conformance-matrix.md"));

        Assert.Contains("`event-sourcing` runtime-surface entries for the seven remaining provider stores", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("`Cephalon.EventSourcing.EntityFramework`", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("`Cephalon.EventSourcing.MongoDB`", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("`Cephalon.EventSourcing.Redis`", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("provider-durable `CephalonEventSnapshots`", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("provider-durable MongoDB latest-snapshot collection", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("provider-durable Redis Hash latest-snapshot", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("All ten providers contribute sanitized descriptors", conformanceMatrix, StringComparison.Ordinal);
    }

    [Fact]
    public void EventSourcingProviderDocsMatchZeroBasedStreamVersionContract()
    {
        var repositoryRoot = GetRepositoryRoot();
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");

        var eventSourcingDocs = Directory
            .GetFiles(componentDocsRoot, "event-sourcing*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(eventSourcingDocs);

        foreach (var docPath in eventSourcingDocs)
        {
            var contents = File.ReadAllText(docPath);

            Assert.DoesNotContain("1-based", contents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stream starts at version 1", contents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("starting at `1`", contents, StringComparison.OrdinalIgnoreCase);
        }

        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var eventEntryFiles = Directory
            .GetDirectories(sourceRoot, "Cephalon.EventSourcing*", SearchOption.TopDirectoryOnly)
            .SelectMany(projectRoot => Directory.GetFiles(projectRoot, "*EventEntry.cs", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(eventEntryFiles);

        foreach (var eventEntryFile in eventEntryFiles)
        {
            var contents = File.ReadAllText(eventEntryFile);

            Assert.DoesNotContain("1-based", contents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stream starts at version 1", contents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("assembly-qualified CLR event type", contents, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fully-qualified CLR event type", contents, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DataProviderRuntimeDocsMatchOutboxInboxSurfaceTruth()
    {
        var repositoryRoot = GetRepositoryRoot();
        var componentDocsRoot = Path.Combine(repositoryRoot, "docs", "components");

        var storeProviderDocs = new[]
        {
            "data-cassandra.md",
            "data-clickhouse.md",
            "data-elasticsearch.md",
            "data-mongodb.md",
            "data-nats.md",
            "data-neo4j.md",
            "data-opensearch.md",
            "data-qdrant.md",
            "data-redis.md"
        };

        foreach (var docFile in storeProviderDocs)
        {
            var componentDoc = File.ReadAllText(Path.Combine(componentDocsRoot, docFile));

            Assert.Contains("`outbox-producers`", componentDoc, StringComparison.Ordinal);
            Assert.Contains("`inbox-stores`", componentDoc, StringComparison.Ordinal);
        }

        foreach (var docFile in new[] { "data-elasticsearch.md", "data-nats.md", "data-neo4j.md", "data-opensearch.md" })
        {
            var componentDoc = File.ReadAllText(Path.Combine(componentDocsRoot, docFile));

            Assert.Contains("uriCredentialsConfigured", componentDoc, StringComparison.Ordinal);
            Assert.Contains("secretProjection = redacted", componentDoc, StringComparison.Ordinal);
        }

        var conformanceMatrix = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "conformance-matrix.md"));
        var maturityAudit = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-surface-maturity-audit.md"));
        var engineComponentDoc = File.ReadAllText(Path.Combine(componentDocsRoot, "engine.md"));
        var runtimeContractIndex = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "runtime-contract-index.md"));

        Assert.Contains("Provider-visible data catalog/runtime truth", conformanceMatrix, StringComparison.Ordinal);
        Assert.Contains("`IOutboxCatalog`, `IInboxCatalog`, `ITechnologyRuntimeCatalog`", conformanceMatrix, StringComparison.Ordinal);
        Assert.DoesNotContain("non-relational data providers (Redis, Neo4j, Cassandra, ClickHouse, Elasticsearch, OpenSearch, Qdrant, Nats, Debezium)", conformanceMatrix, StringComparison.Ordinal);
        Assert.DoesNotContain("from `M1` catalog-only to `M2`", maturityAudit, StringComparison.Ordinal);
        Assert.Contains("shared provider-family keys are additive", engineComponentDoc, StringComparison.Ordinal);
        Assert.Contains("sourceModuleIds", runtimeContractIndex, StringComparison.Ordinal);
    }

    [Fact]
    public void DocsHubLinksToCoreDocumentationSurfaces()
    {
        var docsReadme = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "docs", "README.md"));

        Assert.Contains("[Getting started](getting-started.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Generated app publishing](generated-app-publishing.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Container image publishing](container-image-publishing.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Windows Service deployment](windows-service-deployment.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[IIS deployment](iis-deployment.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Azure App Service deployment](azure-app-service-deployment.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Azure Container Apps deployment](azure-container-apps-deployment.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Kubernetes deployment](kubernetes-deployment.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Linux systemd deployment](linux-systemd-deployment.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Architecture](architecture.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Component catalog](components/README.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[App models](app-models.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Deployment-mode support](deployment-mode-support.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[.NET 11 readiness](dotnet11-readiness.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Module authoring](module-authoring.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Package publishing](package-publishing.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[External package lifecycle](external-package-lifecycle.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Technology packs](technology-packs.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Operations](operations.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Container runtime](container-runtime.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Runtime failure policy](runtime-failure-policy.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Benchmarking](benchmarking.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Reference docs publishing](reference-docs.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Engine roadmap](engine-roadmap.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Engine backlog](engine-backlog.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Reference landing page](reference/README.md)", docsReadme, StringComparison.Ordinal);
    }

    [Fact]
    public void FrameworkReadinessDocsStayAlignedWithDeploymentModeSupportContract()
    {
        var repositoryRoot = GetRepositoryRoot();
        var docsReadme = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "README.md"));
        var compatibility = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "compatibility.md"));
        var deploymentModeSupport = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "deployment-mode-support.md"));
        var dotNet11Readiness = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "dotnet11-readiness.md"));
        var packagePublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "package-publishing.md"));

        Assert.Contains("deployment-mode-support.md", docsReadme, StringComparison.Ordinal);
        Assert.Contains("scripts/deployment-mode-support.json", compatibility, StringComparison.Ordinal);
        Assert.Contains("docs/deployment-mode-support.md", compatibility, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root <path>", compatibility, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor", deploymentModeSupport, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root <path>", deploymentModeSupport, StringComparison.Ordinal);
        Assert.Contains("scripts/deployment-mode-support.json", deploymentModeSupport, StringComparison.Ordinal);
        Assert.Contains("dotnet11-readiness.md", deploymentModeSupport, StringComparison.Ordinal);
        Assert.Contains("deployment-mode-support.md", dotNet11Readiness, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root <path>", dotNet11Readiness, StringComparison.Ordinal);
        Assert.Contains("scripts/deployment-mode-support.json", packagePublishing, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagePublishingDocsStayAlignedWithSignedReleaseDryRunReportContract()
    {
        var repositoryRoot = GetRepositoryRoot();
        var packagePublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "package-publishing.md"));

        AssertDocumentContainsAllTokens(
            packagePublishing,
            ReadSignedReleaseDryRunDocumentationTokens(repositoryRoot),
            "docs/package-publishing.md");
    }

    [Fact]
    public void ReleaseChecklistDocsStayAlignedWithSignedReleaseDryRunReportContract()
    {
        var repositoryRoot = GetRepositoryRoot();
        var requiredFields = ReadSignedReleaseDryRunDocumentationTokens(repositoryRoot);
        var releaseChecklist = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "release-checklist.md"));
        var releaseChecklistTemplate = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "release-checklist-template.md"));

        AssertDocumentContainsAllTokens(releaseChecklist, requiredFields, "docs/release-checklist.md");
        AssertDocumentContainsAllTokens(releaseChecklistTemplate, requiredFields, "docs/release-checklist-template.md");
    }

    [Fact]
    public void CompletionScorecardDocsStayAlignedWithDoctorSummary()
    {
        var repositoryRoot = GetRepositoryRoot();
        var expectedSchemaVersion = ReadScorecardSchemaVersion(repositoryRoot);
        var scorecard = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-completion-scorecard.md"));
        var componentCatalog = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "README.md"));
        var roadmap = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "engine-roadmap.md"));
        var projectMemory = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "project-memory.md"));
        var planningGovernance = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "planning-governance.md"));
        var releaseChecklist = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "release-checklist.md"));
        var releaseChecklistTemplate = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "release-checklist-template.md"));
        var cliComponentDoc = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "cli.md"));
        var cliPackageReadme = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Cephalon.Cli", "PACKAGE.md"));

        Assert.Contains("cephalon doctor --scorecard <path>", scorecard, StringComparison.Ordinal);
        Assert.Contains($"schema version `{expectedSchemaVersion}`", scorecard, StringComparison.Ordinal);
        Assert.Contains("provider integration counts from `ProviderIntegrationEvidence`", scorecard, StringComparison.Ordinal);
        Assert.Contains("adoption-smoke counts from `AdoptionSmokeEvidence`", scorecard, StringComparison.Ordinal);
        Assert.Contains("execution-report default path/schema", scorecard, StringComparison.Ordinal);
        Assert.Contains("golden use cases", scorecard, StringComparison.Ordinal);
        Assert.Contains("golden use-case execution reports", scorecard, StringComparison.Ordinal);
        Assert.Contains("ProviderIntegrationEvidence.DependencyHealthProviderManifest", scorecard, StringComparison.Ordinal);
        Assert.Contains("claims-report path, gate status, target count, warning count, error count, and package-claim verdict counts", scorecard, StringComparison.Ordinal);
        Assert.Contains("boundary/core/full-common/full-operator route-delegate, operator response JSON contract, non-operator endpoint, and framework endpoint boundary audit status/failure counts", scorecard, StringComparison.Ordinal);
        Assert.Contains("Eventing operational-superiority counts from `EventingOperationalSuperiorityEvidence`", scorecard, StringComparison.Ordinal);
        Assert.Contains("code-first hot-path binding mode", scorecard, StringComparison.Ordinal);
        Assert.Contains("SRE posture counts from `SrePostureEvidence`", scorecard, StringComparison.Ordinal);
        Assert.Contains("stable-baseline manifest rows/measurements", scorecard, StringComparison.Ordinal);
        Assert.Contains("pending-baseline blocker evidence rows", scorecard, StringComparison.Ordinal);
        Assert.Contains("signed-release dry-run status/proof/blocker/scope/diagnostic", scorecard, StringComparison.Ordinal);
        Assert.Contains("test coverage counts from `TestCoverageEvidence`", scorecard, StringComparison.Ordinal);
        Assert.Contains($"scorecard schema `{expectedSchemaVersion}`", componentCatalog, StringComparison.Ordinal);
        Assert.Contains("ProviderIntegrationEvidence", componentCatalog, StringComparison.Ordinal);
        Assert.Contains("ProviderIntegrationEvidence.DependencyHealthProviderManifest", componentCatalog, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard", componentCatalog, StringComparison.Ordinal);
        Assert.Contains($"scorecard schema `{expectedSchemaVersion}`", roadmap, StringComparison.Ordinal);
        Assert.Contains("SupplyChainEvidence.SignedReleaseDryRun", roadmap, StringComparison.Ordinal);
        Assert.Contains("signed-release dry-run", roadmap, StringComparison.Ordinal);
        Assert.Contains("EventingOperationalSuperiorityEvidence", roadmap, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard", roadmap, StringComparison.Ordinal);
        Assert.Contains($"generated artifact is now schema `{expectedSchemaVersion}`", projectMemory, StringComparison.Ordinal);
        Assert.Contains($"currently requires scorecard schema `{expectedSchemaVersion}`", projectMemory, StringComparison.Ordinal);
        Assert.Contains("deployment-mode claims-report gate/target/warning/error/package-claim verdict counts", projectMemory, StringComparison.Ordinal);
        Assert.Contains("boundary/core/full-common/full-operator-route-delegate/operator-response-JSON/non-operator-endpoint/framework-endpoint-boundary audit counts", projectMemory, StringComparison.Ordinal);
        Assert.Contains("adoption-smoke execution-report counts/path", projectMemory, StringComparison.Ordinal);
        Assert.Contains("AdoptionSmokeEvidence.ExecutionReport", projectMemory, StringComparison.Ordinal);
        Assert.Contains("validates provider-integration evidence from `scripts/provider-integration-support.json`", projectMemory, StringComparison.Ordinal);
        Assert.Contains("ProviderIntegrationEvidence", projectMemory, StringComparison.Ordinal);
        Assert.Contains("ProviderIntegrationEvidence.DependencyHealthProviderManifest", projectMemory, StringComparison.Ordinal);
        Assert.Contains("scripts/sre-stable-baselines.json", projectMemory, StringComparison.Ordinal);
        Assert.Contains("provider live/composition/gate/runtime-contract posture", projectMemory, StringComparison.Ordinal);
        Assert.Contains("scripts/eventing-operational-superiority-support.json", projectMemory, StringComparison.Ordinal);
        Assert.Contains("EventingOperationalSuperiorityEvidence", projectMemory, StringComparison.Ordinal);
        Assert.Contains("validates `docs/test-coverage-roadmap.md` into `TestCoverageEvidence`", projectMemory, StringComparison.Ordinal);
        Assert.Contains("test coverage counts from `TestCoverageEvidence`", projectMemory, StringComparison.Ordinal);
        Assert.Contains("claims-report gate/target/warning/error/package-claim", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("adoption-smoke execution-report, golden use-case, and per-use-case report readback", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("claims-report gate/target/warning/error/package-claim", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("provider integration counts from `ProviderIntegrationEvidence`", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("dependency-health provider-manifest readback", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("provider integration row/live/composition counts", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("dependency-health provider-manifest readback", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Eventing operational-superiority promotion contract/status/dimension/coverage/hot-path/Wolverine", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Eventing operational-superiority promotion contract/status/dimension/coverage/hot-path/Wolverine", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("SRE posture plus stable-baseline manifest, pending-baseline blocker, and guardrail-coverage counts", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("SRE SLI/target/baseline/stable-baseline-manifest/pending-baseline-blocker/guardrail-coverage counts", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("signed-release dry-run status/proof/blocker/scope/diagnostic/command/output", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("signed-release dry-run status/proof/blocker/scope/diagnostic/command/output", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("test coverage counts from `TestCoverageEvidence`", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("test coverage layered-project/gap/recommendation/quarantine counts from `TestCoverageEvidence`", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard <path>", planningGovernance, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard artifacts/engine-completion-scorecard-release/engine-completion-scorecard.json", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard artifacts/engine-completion-scorecard-release/engine-completion-scorecard.json", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("dependency-health provider-manifest", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("dependency-health provider-manifest", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("deployment-mode claims-report", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("deployment-mode claims-report", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("release-validation and optional `cephalon doctor --scorecard", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("release-validation and optional `cephalon doctor --scorecard", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("signed-release dry-run", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("signed-release dry-run", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("TestCoverageEvidence", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("TestCoverageEvidence", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("EventingOperationalSuperiorityEvidence", releaseChecklist, StringComparison.Ordinal);
        Assert.Contains("EventingOperationalSuperiorityEvidence", releaseChecklistTemplate, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard <path>", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --scorecard <path>", cliPackageReadme, StringComparison.Ordinal);
    }

    [Fact]
    public void AdoptionGuideAndPackageReadmesStayAlignedWithDoctorPath()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStarted = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "getting-started.md"));
        var generatedAppPublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "generated-app-publishing.md"));
        var containerImagePublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "container-image-publishing.md"));
        var windowsServiceDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "windows-service-deployment.md"));
        var iisDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "iis-deployment.md"));
        var azureAppServiceDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "azure-app-service-deployment.md"));
        var azureContainerAppsDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "azure-container-apps-deployment.md"));
        var kubernetesDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "kubernetes-deployment.md"));
        var linuxSystemdDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "linux-systemd-deployment.md"));
        var referenceDocsGuide = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "reference-docs.md"));
        var cliComponentDoc = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "cli.md"));
        var cliPackageReadme = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Cephalon.Cli", "PACKAGE.md"));
        var templatePackReadme = File.ReadAllText(Path.Combine(repositoryRoot, "templates", "Cephalon.TemplatePack", "PACKAGE.md"));
        var rootReadme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));

        Assert.Contains("cephalon doctor", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assessment-only", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Program.cs", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("AddCephalonProjectConfigurations", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("MapCephalon", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("PackageReference", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.SourceGen", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Configurations/**/*.json", gettingStarted, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages/README.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Configurations/AddEngine.*.json", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Configurations/Observability/Development.json", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("AddOpenApi.json", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("AddReferenceDocs.json", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("development Serilog", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("self-hosted and hosted deployment assets", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generated Dockerfile SDK/runtime image tags", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("PublishTrimmed", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("PublishAot", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("PublishSingleFile", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("local orchestration assets", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("otel-collector-config.yaml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image/publish-image.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("windows-service-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/remove-service.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("iis-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/remove-site.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("azure-app-service-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("azure-container-apps-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("kubernetes-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/kustomization.yaml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/namespace.yaml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/deployment.yaml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/service.yaml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages/README.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/kustomization.yaml", rootReadme, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages/README.md", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/kustomization.yaml", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages/README.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/namespace.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages/README.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/deployment.yaml", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("linux-systemd-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-systemd.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd/Acme.Store.env", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("dotnet new list cephalon", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("CompositionSmokeTests.cs", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("BehaviorSpecifications.cs", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Given/When/Then", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("test harness", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generated guidance docs", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Configurations/README.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/README.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/install-service.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/remove-service.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("windows-service-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image/publish-image.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/install-site.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/remove-site.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("iis-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service/deploy-zip.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("azure-app-service-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("azure-container-apps-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("kubernetes-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("self-hosted and hosted deployment assets", generatedAppPublishing, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("linux-systemd-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("Acme.Store.env", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("dotnet publish", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("publish-image.ps1", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("docker push", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("sc.exe create", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("install-service.ps1", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("remove-service.ps1", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("AspNetCoreModuleV2", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("install-site.ps1", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("remove-site.ps1", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", azureAppServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", azureAppServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("az webapp deploy", azureAppServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", azureContainerAppsDeployment, StringComparison.Ordinal);
        Assert.Contains("az containerapp up", azureContainerAppsDeployment, StringComparison.Ordinal);
        Assert.Contains("--source", azureContainerAppsDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", kubernetesDeployment, StringComparison.Ordinal);
        Assert.Contains("kubectl kustomize", kubernetesDeployment, StringComparison.Ordinal);
        Assert.Contains("ClusterIP", kubernetesDeployment, StringComparison.Ordinal);
        Assert.Contains("systemd-analyze verify", linuxSystemdDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-systemd.ps1", linuxSystemdDeployment, StringComparison.Ordinal);
        Assert.Contains(".env", linuxSystemdDeployment, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target framework", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Program.cs", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("AddCephalonProjectConfigurations", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("MapCephalon", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("PackageReference", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.SourceGen", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/**/*.json", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("PublishTrimmed", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("PublishAot", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("PublishSingleFile", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("not-claimed", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/AddEngine.*.json", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/Observability/Development.json", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("AddOpenApi.json", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("AddReferenceDocs.json", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("self-hosted and hosted deployment assets", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deploy/windows-service/remove-service.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/remove-site.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd/Acme.Store.env", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image/publish-image.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/kustomization.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/namespace.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/deployment.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/service.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("getting-started.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("generated-app-publishing.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("windows-service-deployment.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("iis-deployment.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("azure-app-service-deployment.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("azure-container-apps-deployment.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("kubernetes-deployment.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("linux-systemd-deployment.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("generated Dockerfile base-image alignment", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CompositionSmokeTests.cs", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("BehaviorSpecifications.cs", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("Given/When/Then", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("generated guidance docs", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Configurations/README.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/README.md", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", templatePackReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Program.cs", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("AddCephalonProjectConfigurations", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("MapCephalon", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("PackageReference", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.SourceGen", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/**/*.json", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/AddEngine.*.json", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/Observability/Development.json", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("AddOpenApi.json", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("AddReferenceDocs.json", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("self-hosted and hosted deployment assets", templatePackReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deploy/container-image/publish-image.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("generated host target framework", templatePackReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generated Dockerfile baseline", templatePackReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generated guidance docs", templatePackReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Configurations/README.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/README.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("generated guidance docs", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deploy/container-image/publish-image.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Configurations/README.md", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/README.md", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("generated guidance docs", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deploy/container-image/publish-image.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/README.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("dotnet new list cephalon", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("generated-app-publishing.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("windows-service-deployment.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("iis-deployment.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/iis", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("azure-app-service-deployment.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("azure-container-apps-deployment.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("kubernetes-deployment.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("linux-systemd-deployment.md", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("CompositionSmokeTests.cs", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("BehaviorSpecifications.cs", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("Given/When/Then", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("docs/getting-started.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/generated-app-publishing.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/container-image-publishing.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/windows-service-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/iis-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/azure-app-service-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/azure-container-apps-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/kubernetes-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/linux-systemd-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor", rootReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Program.cs", rootReadme, StringComparison.Ordinal);
        Assert.Contains("AddCephalonProjectConfigurations", rootReadme, StringComparison.Ordinal);
        Assert.Contains("MapCephalon", rootReadme, StringComparison.Ordinal);
        Assert.Contains("PackageReference", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.SourceGen", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/**/*.json", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/AddEngine.*.json", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/Observability/Development.json", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/AddOpenApi.json", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Configurations/AddReferenceDocs.json", rootReadme, StringComparison.Ordinal);
        Assert.Contains("self-hosted and hosted deployment assets", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Dockerfile baseline", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("publish-claim posture", rootReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CephalonFolder.pubxml", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("Program.cs", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("AddCephalonProjectConfigurations", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("MapCephalon", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("PackageReference", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Engine.SourceGen", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Configurations/**/*.json", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("CompositionSmokeTests.cs", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("BehaviorSpecifications.cs", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Given/When/Then", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("test harness", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose -f ./Acme.Store/compose.yaml up --build", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("compose.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("docker compose up --build", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", gettingStarted, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("AddReferenceDocs.json", referenceDocsGuide, StringComparison.Ordinal);
        Assert.Contains("AddOpenApi.json", referenceDocsGuide, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root <path>", referenceDocsGuide, StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalPackageDocsStayAlignedWithPackageStageFlow()
    {
        var repositoryRoot = GetRepositoryRoot();
        var docsReadme = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "README.md"));
        var packagePublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "package-publishing.md"));
        var moduleAuthoring = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "module-authoring.md"));
        var packageLifecycle = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "external-package-lifecycle.md"));
        var referenceModuleReadme = File.ReadAllText(Path.Combine(repositoryRoot, "samples", "Cephalon.ReferenceModule.Operations", "README.md"));
        var cliComponentDoc = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "cli.md"));
        var cliPackageReadme = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Cephalon.Cli", "PACKAGE.md"));

        Assert.Contains("[External package lifecycle](external-package-lifecycle.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", packagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", packagePublishing, StringComparison.Ordinal);
        Assert.Contains("external-package-lifecycle.md", packagePublishing, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", moduleAuthoring, StringComparison.Ordinal);
        Assert.Contains("Engine:PackagePolicy", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("Engine:Trust", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("/engine/packages", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", referenceModuleReadme, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", referenceModuleReadme, StringComparison.Ordinal);
        Assert.Contains("Engine:Discovery", referenceModuleReadme, StringComparison.Ordinal);
        Assert.Contains("external package staging", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", cliPackageReadme, StringComparison.Ordinal);
    }

    [Fact]
    public void ContainerRuntimeDocsStayAlignedWithSampleAssets()
    {
        var repositoryRoot = GetRepositoryRoot();
        var docsReadme = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "README.md"));
        var gettingStarted = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "getting-started.md"));
        var generatedAppPublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "generated-app-publishing.md"));
        var containerImagePublishing = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "container-image-publishing.md"));
        var windowsServiceDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "windows-service-deployment.md"));
        var iisDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "iis-deployment.md"));
        var azureAppServiceDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "azure-app-service-deployment.md"));
        var azureContainerAppsDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "azure-container-apps-deployment.md"));
        var kubernetesDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "kubernetes-deployment.md"));
        var containerRuntime = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "container-runtime.md"));
        var linuxSystemdDeployment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "linux-systemd-deployment.md"));
        var operations = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "operations.md"));
        var rootReadme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));
        var sampleReadme = File.ReadAllText(Path.Combine(repositoryRoot, "samples", "Cephalon.Sample.ModularMonolith", "README.md"));

        Assert.Contains("[Container runtime](container-runtime.md)", docsReadme, StringComparison.Ordinal);
        Assert.Contains("[Container runtime](container-runtime.md)", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Docker Desktop", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("WSL", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("validate-container-runtime.ps1", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("compose.packages.yaml", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("/health/ready", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("validate-container-runtime.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-systemd.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("docs/container-runtime.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/container-image-publishing.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/windows-service-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/iis-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/azure-app-service-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/azure-container-apps-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/kubernetes-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docs/linux-systemd-deployment.md", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-container-runtime.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-systemd.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("docker push", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("az webapp deploy", azureAppServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("az containerapp up", azureContainerAppsDeployment, StringComparison.Ordinal);
        Assert.Contains("kubectl kustomize", kubernetesDeployment, StringComparison.Ordinal);
        Assert.Contains("systemd-analyze verify", linuxSystemdDeployment, StringComparison.Ordinal);
        Assert.Contains("compose.yaml", sampleReadme, StringComparison.Ordinal);
        Assert.Contains("otel-collector", sampleReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon new", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("dotnet new", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("endpoint unset", containerRuntime, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", containerRuntime, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages", containerRuntime, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedAppContainerDocsStayAlignedAcrossCliScaffoldingAndTemplatePack()
    {
        var repositoryRoot = GetRepositoryRoot();
        var rootReadme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));
        var scaffoldingComponentDoc = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "scaffolding.md"));
        var cliComponentDoc = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "components", "cli.md"));
        var cliPackageReadme = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Cephalon.Cli", "PACKAGE.md"));
        var templatePackReadme = File.ReadAllText(Path.Combine(repositoryRoot, "templates", "Cephalon.TemplatePack", "PACKAGE.md"));

        Assert.Contains("compose.yaml", rootReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon new", rootReadme, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", rootReadme, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/iis", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes", rootReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd", rootReadme, StringComparison.Ordinal);
        Assert.Contains("compose.yaml", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.OpenTelemetry", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/iis", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd", scaffoldingComponentDoc, StringComparison.Ordinal);
        Assert.Contains("container assets", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("local orchestration assets", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("self-hosted and hosted deployment assets", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose up --build", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Windows Service", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("IIS", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Azure App Service", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("container-image publish", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Azure Container Apps", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("Kubernetes", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("systemd-analyze", cliComponentDoc, StringComparison.Ordinal);
        Assert.Contains("otel-collector-config.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("local orchestration assets", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose up --build", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/iis", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("otel-collector-config.yaml", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("local orchestration assets", templatePackReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("endpoint unset", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/iis", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deploy/linux/systemd", templatePackReadme, StringComparison.Ordinal);
    }

    private static string ReadScorecardSchemaVersion(string repositoryRoot)
    {
        var script = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "publish-engine-completion-scorecard.ps1"));
        const string marker = "$Script:SchemaVersion = \"";
        var markerIndex = script.IndexOf(marker, StringComparison.Ordinal);

        Assert.True(markerIndex >= 0, "Expected publish-engine-completion-scorecard.ps1 to declare $Script:SchemaVersion.");

        var startIndex = markerIndex + marker.Length;
        var endIndex = script.IndexOf('"', startIndex);

        Assert.True(endIndex > startIndex, "Expected publish-engine-completion-scorecard.ps1 schema version to be quoted.");

        return script[startIndex..endIndex];
    }

    private static string ReadDocumentBaselineDate(string document, string prefix)
    {
        var marker = prefix + " `";
        var markerIndex = document.IndexOf(marker, StringComparison.Ordinal);

        Assert.True(markerIndex >= 0, $"Expected document to declare baseline date with prefix '{prefix}'.");

        var startIndex = markerIndex + marker.Length;
        var endIndex = document.IndexOf('`', startIndex);

        Assert.True(endIndex > startIndex, $"Expected document baseline date after prefix '{prefix}' to be quoted.");

        return document[startIndex..endIndex];
    }

    private static int CountOrdinalOccurrences(string source, string value)
    {
        var count = 0;
        var startIndex = 0;

        while (startIndex < source.Length)
        {
            var index = source.IndexOf(value, startIndex, StringComparison.Ordinal);
            if (index < 0)
                return count;

            count++;
            startIndex = index + value.Length;
        }

        return count;
    }

    private static bool IsRepositoryLocalLink(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return false;

        if (target.StartsWith('#'))
            return true;

        return !Uri.TryCreate(target, UriKind.Absolute, out _);
    }

    private static bool IsRepositoryLocalBrowserReference(string target)
    {
        return !string.IsNullOrWhiteSpace(target) &&
               !target.StartsWith('#') &&
               IsRepositoryLocalLink(target);
    }

    private static IEnumerable<string> EnumerateHandAuthoredMarkdownPaths(string repositoryRoot)
    {
        return Directory
            .EnumerateFiles(repositoryRoot, "*.md", SearchOption.AllDirectories)
            .Where(path => IsHandAuthoredMarkdownPath(repositoryRoot, path));
    }

    private static IEnumerable<string> EnumerateReferenceMarkdownPaths(string referenceDocsRoot)
    {
        return Directory.EnumerateFiles(referenceDocsRoot, "*.md", SearchOption.AllDirectories);
    }

    private static string AssertReferenceBundleFileExists(string referenceDocsRoot, string fileName, string description)
    {
        Assert.False(string.IsNullOrWhiteSpace(fileName), $"Expected {description} to declare a non-empty file name.");
        Assert.False(Path.IsPathFullyQualified(fileName), $"Expected {description} '{fileName}' to be relative to docs/reference.");

        var resolvedPath = Path.GetFullPath(Path.Combine(referenceDocsRoot, fileName.Replace('/', Path.DirectorySeparatorChar)));
        Assert.True(
            IsPathInsideDirectory(referenceDocsRoot, resolvedPath),
            $"Expected {description} '{fileName}' to stay inside docs/reference but resolved to '{resolvedPath}'.");
        Assert.True(
            File.Exists(resolvedPath),
            $"Expected {description} '{fileName}' to resolve to an existing generated reference-doc file at '{resolvedPath}'.");

        return resolvedPath;
    }

    private static void AssertReferenceManifestAnchorsResolve(
        string referenceDocsRoot,
        IReadOnlyCollection<JsonElement> entries,
        IReadOnlySet<string> assemblyFiles,
        Dictionary<string, HashSet<string>> anchorCache,
        string entryDescription)
    {
        foreach (var entry in entries)
        {
            var fileName = ReadRequiredJsonString(entry, "FileName", $"reference manifest {entryDescription}");
            var anchorId = ReadRequiredJsonString(entry, "AnchorId", $"reference manifest {entryDescription}");

            Assert.Contains(fileName, assemblyFiles);
            var resolvedPath = AssertReferenceBundleFileExists(referenceDocsRoot, fileName, $"reference manifest {entryDescription}");
            var anchors = ReadCachedMarkdownAnchors(resolvedPath, anchorCache);

            Assert.True(
                anchors.Contains(anchorId),
                $"Expected reference manifest {entryDescription} anchor '{anchorId}' in '{fileName}' to resolve to a generated Markdown anchor.");
        }
    }

    private static void AssertManifestEntryCountsMatchAssemblies(
        string entryDescription,
        IReadOnlyCollection<JsonElement> entries,
        IReadOnlyDictionary<string, int> expectedCounts)
    {
        var actualCounts = entries
            .Select(entry => ReadRequiredJsonString(entry, "AssemblyName", $"reference manifest {entryDescription}"))
            .GroupBy(assemblyName => assemblyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var expectedCount in expectedCounts)
        {
            actualCounts.TryGetValue(expectedCount.Key, out var actualCount);
            Assert.Equal(expectedCount.Value, actualCount);
        }

        foreach (var actualCount in actualCounts)
            Assert.Contains(actualCount.Key, expectedCounts.Keys);
    }

    private static HashSet<string> ReadCachedMarkdownAnchors(
        string markdownPath,
        Dictionary<string, HashSet<string>> anchorCache)
    {
        if (!anchorCache.TryGetValue(markdownPath, out var anchors))
        {
            anchors = ReadMarkdownAnchors(markdownPath);
            anchorCache[markdownPath] = anchors;
        }

        return anchors;
    }

    private static string[] ReadSignedReleaseDryRunDocumentationTokens(string repositoryRoot)
    {
        using var supplyChainManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot,
            "scripts",
            "supply-chain-release-support.json")));

        var root = supplyChainManifest.RootElement;
        Assert.Equal("1.7.0", ReadRequiredJsonString(root, "$schemaVersion", "supply-chain release support manifest"));
        Assert.True(
            root.TryGetProperty("signedReleaseDryRun", out var signedReleaseDryRun),
            "Expected supply-chain release support manifest to define signedReleaseDryRun.");
        Assert.True(
            signedReleaseDryRun.TryGetProperty("requiredReportFields", out var requiredReportFields),
            "Expected signedReleaseDryRun to define requiredReportFields.");
        var handoffOutputPath = ReadRequiredJsonString(
            signedReleaseDryRun,
            "handoffOutputPath",
            "signed-release dry-run handoff output path");

        var requiredFields = requiredReportFields
            .EnumerateArray()
            .Select(field => field.GetString())
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field => field!)
            .ToArray();
        Assert.NotEmpty(requiredFields);
        return requiredFields
            .Append(handoffOutputPath)
            .ToArray();
    }

    private static void AssertDocumentContainsAllTokens(
        string document,
        IEnumerable<string> requiredTokens,
        string documentDescription)
    {
        foreach (var requiredToken in requiredTokens)
        {
            Assert.True(
                document.Contains(requiredToken, StringComparison.Ordinal),
                $"Expected {documentDescription} to contain required token '{requiredToken}'.");
        }
    }

    private static string ReadRequiredJsonString(JsonElement element, string propertyName, string description)
    {
        Assert.True(
            element.TryGetProperty(propertyName, out var property),
            $"Expected {description} to contain a '{propertyName}' property.");
        Assert.Equal(JsonValueKind.String, property.ValueKind);

        var value = property.GetString();
        Assert.False(string.IsNullOrWhiteSpace(value), $"Expected {description} '{propertyName}' to be non-empty.");
        return value!;
    }

    private static int ReadRequiredJsonInt32(JsonElement element, string propertyName, string description)
    {
        Assert.True(
            element.TryGetProperty(propertyName, out var property),
            $"Expected {description} to contain a '{propertyName}' property.");
        Assert.Equal(JsonValueKind.Number, property.ValueKind);

        return property.GetInt32();
    }

    private static bool IsHandAuthoredMarkdownPath(string repositoryRoot, string markdownPath)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, markdownPath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (segments.Any(static segment =>
                string.Equals(segment, ".git", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return !(segments.Length >= 2 &&
                 string.Equals(segments[0], "docs", StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(segments[1], "reference", StringComparison.OrdinalIgnoreCase));
    }

    private static int AssertLocalMarkdownLinksResolve(
        string repositoryRoot,
        string markdownPath,
        string documentDescription,
        bool allowDirectoryTargets = false,
        bool requireLocalLinks = true,
        string? requiredContainingDirectory = null)
    {
        var markdownRoot = Path.GetDirectoryName(markdownPath)!;
        var markdown = RemoveMarkdownCode(File.ReadAllText(markdownPath));
        var localLinks = MarkdownLinkPattern
            .Matches(markdown)
            .Where(match => !IsInsideInlineCodeSpan(markdown, match.Index))
            .Select(static match => match.Groups["target"].Value.Trim())
            .Where(IsRepositoryLocalLink)
            .Select(ParseLocalMarkdownLink)
            .GroupBy(static link => link.OriginalTarget, StringComparer.Ordinal)
            .Select(static group => group.First())
            .OrderBy(static link => link.OriginalTarget, StringComparer.Ordinal)
            .ToArray();

        if (requireLocalLinks)
            Assert.NotEmpty(localLinks);

        foreach (var localLink in localLinks)
        {
            var resolvedPath = ResolveLocalMarkdownLinkPath(markdownRoot, markdownPath, localLink.TargetPath);

            Assert.True(
                IsPathInsideRepository(repositoryRoot, resolvedPath),
                $"Expected {documentDescription} link target '{localLink.OriginalTarget}' to stay inside the repository but resolved to '{resolvedPath}'.");

            if (!string.IsNullOrWhiteSpace(requiredContainingDirectory))
            {
                Assert.True(
                    IsPathInsideDirectory(requiredContainingDirectory, resolvedPath),
                    $"Expected {documentDescription} link target '{localLink.OriginalTarget}' to stay inside '{requiredContainingDirectory}' but resolved to '{resolvedPath}'.");
            }

            Assert.True(
                File.Exists(resolvedPath) || (allowDirectoryTargets && Directory.Exists(resolvedPath)),
                allowDirectoryTargets
                    ? $"Expected {documentDescription} link target '{localLink.OriginalTarget}' to resolve to an existing file or directory at '{resolvedPath}'."
                    : $"Expected {documentDescription} link target '{localLink.OriginalTarget}' to resolve to an existing file at '{resolvedPath}'.");

            AssertLocalMarkdownFragmentResolves(documentDescription, localLink, resolvedPath);
        }

        return localLinks.Length;
    }

    private static MarkdownLocalLink ParseLocalMarkdownLink(string target)
    {
        var normalizedTarget = target.Trim();

        if (normalizedTarget.Length >= 2 &&
            normalizedTarget[0] == '<' &&
            normalizedTarget[^1] == '>')
        {
            normalizedTarget = normalizedTarget[1..^1];
        }

        var fragmentSeparatorIndex = normalizedTarget.IndexOf('#', StringComparison.Ordinal);
        var targetPath = fragmentSeparatorIndex >= 0 ? normalizedTarget[..fragmentSeparatorIndex] : normalizedTarget;
        var fragment = fragmentSeparatorIndex >= 0 ? normalizedTarget[(fragmentSeparatorIndex + 1)..] : null;
        var querySeparatorIndex = targetPath.IndexOf('?', StringComparison.Ordinal);

        if (querySeparatorIndex >= 0)
            targetPath = targetPath[..querySeparatorIndex];

        return new MarkdownLocalLink(
            target,
            Uri.UnescapeDataString(targetPath),
            fragment is null ? null : Uri.UnescapeDataString(fragment));
    }

    private static string ResolveLocalMarkdownLinkPath(string markdownRoot, string markdownPath, string targetPath)
    {
        if (targetPath.Length == 0)
            return Path.GetFullPath(markdownPath);

        var normalizedTarget = targetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(markdownRoot, normalizedTarget));
    }

    private static void AssertLocalMarkdownFragmentResolves(
        string documentDescription,
        MarkdownLocalLink localLink,
        string resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(localLink.Fragment))
            return;

        var normalizedFragment = localLink.Fragment.Trim();

        if (GitHubLineFragmentPattern.IsMatch(normalizedFragment))
        {
            AssertGitHubLineFragmentResolves(documentDescription, localLink, resolvedPath, normalizedFragment);
            return;
        }

        Assert.True(
            File.Exists(resolvedPath) &&
            string.Equals(Path.GetExtension(resolvedPath), ".md", StringComparison.OrdinalIgnoreCase),
            $"Expected {documentDescription} link target '{localLink.OriginalTarget}' fragment '#{localLink.Fragment}' to point at a Markdown heading anchor or GitHub line fragment, but '{resolvedPath}' is not a Markdown file.");

        var anchors = ReadMarkdownAnchors(resolvedPath);
        var normalizedAnchorReference = NormalizeMarkdownAnchorReference(normalizedFragment);

        Assert.True(
            anchors.Contains(normalizedAnchorReference),
            $"Expected {documentDescription} link target '{localLink.OriginalTarget}' fragment '#{localLink.Fragment}' to match a Markdown heading or explicit HTML anchor in '{resolvedPath}'.");
    }

    private static void AssertGitHubLineFragmentResolves(
        string documentDescription,
        MarkdownLocalLink localLink,
        string resolvedPath,
        string normalizedFragment)
    {
        Assert.True(
            File.Exists(resolvedPath),
            $"Expected {documentDescription} link target '{localLink.OriginalTarget}' line fragment '#{localLink.Fragment}' to point at an existing file.");

        var match = GitHubLineFragmentPattern.Match(normalizedFragment);
        var startLine = int.Parse(match.Groups["start"].Value, CultureInfo.InvariantCulture);
        var endLine = match.Groups["end"].Success
            ? int.Parse(match.Groups["end"].Value, CultureInfo.InvariantCulture)
            : startLine;
        var lineCount = File.ReadLines(resolvedPath).Count();

        Assert.True(
            startLine > 0 && endLine >= startLine && endLine <= lineCount,
            $"Expected {documentDescription} link target '{localLink.OriginalTarget}' line fragment '#{localLink.Fragment}' to fit within '{resolvedPath}' ({lineCount} line(s)).");
    }

    private static HashSet<string> ReadMarkdownAnchors(string markdownPath)
    {
        var markdown = RemoveMarkdownCode(File.ReadAllText(markdownPath));
        var anchors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in MarkdownHeadingPattern.Matches(markdown))
        {
            if (IsInsideInlineCodeSpan(markdown, match.Index))
                continue;

            var baseAnchor = CreateGitHubMarkdownHeadingAnchor(match.Groups["text"].Value);

            if (baseAnchor.Length == 0)
                continue;

            var duplicateCount = duplicateCounts.GetValueOrDefault(baseAnchor);
            duplicateCounts[baseAnchor] = duplicateCount + 1;

            anchors.Add(duplicateCount == 0 ? baseAnchor : $"{baseAnchor}-{duplicateCount}");
        }

        foreach (Match match in HtmlAnchorPattern.Matches(markdown))
            anchors.Add(match.Groups["anchor"].Value.Trim());

        return anchors;
    }

    private static string CreateGitHubMarkdownHeadingAnchor(string headingText)
    {
        var visibleText = MarkdownInlineLinkTextPattern.Replace(headingText, "${text}");
        visibleText = HtmlTagPattern.Replace(visibleText, string.Empty);
        visibleText = visibleText.Replace("`", string.Empty, StringComparison.Ordinal);
        visibleText = visibleText.Trim().ToLowerInvariant();

        var anchor = new StringBuilder(visibleText.Length);

        foreach (var character in visibleText)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                anchor.Append(character);
                continue;
            }

            if (char.IsWhiteSpace(character) || character == '-')
                anchor.Append('-');
        }

        return anchor.ToString().Trim('-');
    }

    private static string NormalizeMarkdownAnchorReference(string fragment)
    {
        return fragment.Trim().ToLowerInvariant();
    }

    private static string RemoveMarkdownCode(string markdown)
    {
        return FencedCodeBlockPattern.Replace(markdown, Environment.NewLine);
    }

    private static bool IsInsideInlineCodeSpan(string markdown, int index)
    {
        var lineStart = markdown.LastIndexOf('\n', Math.Max(0, index - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        var codeSpanDelimiterLength = 0;

        for (var i = lineStart; i < index;)
        {
            if (markdown[i] != '`')
            {
                i++;
                continue;
            }

            if (i > 0 && markdown[i - 1] == '\\')
            {
                i++;
                continue;
            }

            var delimiterStart = i;

            while (i < index && markdown[i] == '`')
                i++;

            var delimiterLength = i - delimiterStart;

            if (codeSpanDelimiterLength == 0)
                codeSpanDelimiterLength = delimiterLength;
            else if (delimiterLength == codeSpanDelimiterLength)
                codeSpanDelimiterLength = 0;
        }

        return codeSpanDelimiterLength != 0;
    }

    private static bool IsPathInsideRepository(string repositoryRoot, string path)
    {
        return IsPathInsideDirectory(repositoryRoot, path);
    }

    private static bool IsPathInsideDirectory(string directoryPath, string path)
    {
        var normalizedDirectoryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directoryPath));
        var normalizedPath = Path.GetFullPath(path);

        return normalizedPath.Equals(normalizedDirectoryPath, StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(normalizedDirectoryPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct MarkdownLocalLink(
        string OriginalTarget,
        string TargetPath,
        string? Fragment);

    private static DependencyHealthProviderManifestRow[] ReadDependencyHealthProviderManifest(string repositoryRoot)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot,
            "scripts",
            "observability-dependency-health-providers.json")));
        var root = manifest.RootElement;
        Assert.Equal("1.0.0", ReadRequiredString(root, "schemaVersion"));

        var providers = root.GetProperty("providers").EnumerateArray()
            .Select(provider => new DependencyHealthProviderManifestRow(
                ReadRequiredString(provider, "provider"),
                ReadRequiredString(provider, "source"),
                ReadRequiredString(provider, "id"),
                ReadRequiredString(provider, "displayName"),
                ReadRequiredString(provider, "configurationSection"),
                ReadRequiredString(provider, "extensionMethod"),
                ReadRequiredString(provider, "definitionType"),
                ReadRequiredString(provider, "optionsType"),
                ReadRequiredString(provider, "componentDoc"),
                ReadRequiredString(provider, "hostedServiceFile"),
                ReadRequiredString(provider, "diagnosticsContributorFile"),
                ReadRequiredString(provider, "configurationOptionsFile"),
                ReadRequiredString(provider, "hostingExtensionFile"),
                ReadRequiredString(provider, "maturity"),
                ReadRequiredString(provider, "ownership")))
            .ToArray();

        Assert.Equal(root.GetProperty("providerCount").GetInt32(), providers.Length);

        return providers;
    }

    private static string NormalizeRepositoryPath(string path) => path.Replace('/', Path.DirectorySeparatorChar);

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        var value = element.GetProperty(propertyName).GetString();
        Assert.False(string.IsNullOrWhiteSpace(value), $"Expected manifest property '{propertyName}' to be populated.");
        return value!;
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

    private sealed record DependencyHealthProviderManifestRow(
        string Provider,
        string Source,
        string Id,
        string DisplayName,
        string ConfigurationSection,
        string ExtensionMethod,
        string DefinitionType,
        string OptionsType,
        string ComponentDoc,
        string HostedServiceFile,
        string DiagnosticsContributorFile,
        string ConfigurationOptionsFile,
        string HostingExtensionFile,
        string Maturity,
        string Ownership)
    {
        public string ConfigurationSectionName => ConfigurationSection.Split(':')[^1];
    }
}
