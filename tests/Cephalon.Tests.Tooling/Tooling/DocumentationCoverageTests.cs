namespace Cephalon.Tests.Tooling;

public sealed class DocumentationCoverageTests
{
    // Projects that are internal implementation helpers (IsPackable=false) — not shipped as NuGet packages
    // and therefore do not require component documentation.
    private static readonly HashSet<string> NonPackableProjects = new(StringComparer.Ordinal)
    {
        "Cephalon.Observability.DependencyHealth.Core"
    };

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
            Assert.Contains(
                $"[{projectName}]({componentDocFileName})",
                componentCatalog,
                StringComparison.Ordinal);
        }
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
        Assert.Contains("scripts/deployment-mode-support.json", deploymentModeSupport, StringComparison.Ordinal);
        Assert.Contains("dotnet11-readiness.md", deploymentModeSupport, StringComparison.Ordinal);
        Assert.Contains("deployment-mode-support.md", dotNet11Readiness, StringComparison.Ordinal);
        Assert.Contains("scripts/deployment-mode-support.json", packagePublishing, StringComparison.Ordinal);
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
        var cliPackageReadme = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Cephalon.Cli", "PACKAGE.md"));
        var templatePackReadme = File.ReadAllText(Path.Combine(repositoryRoot, "templates", "Cephalon.TemplatePack", "PACKAGE.md"));
        var rootReadme = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));

        Assert.Contains("cephalon doctor", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", gettingStarted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assessment-only", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("windows-service-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("iis-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("azure-app-service-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("azure-container-apps-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("kubernetes-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("linux-systemd-deployment.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-systemd.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("dotnet new list cephalon", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("CompositionSmokeTests.cs", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("BehaviorSpecifications.cs", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("CephalonFolder.pubxml", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/windows-service/install-service.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("windows-service-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image/publish-image.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("container-image-publishing.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/iis/install-site.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("iis-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-app-service/deploy-zip.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("azure-app-service-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("azure-container-apps-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/kubernetes/apply.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("kubernetes-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-publish.ps1", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("linux-systemd-deployment.md", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("dotnet publish", generatedAppPublishing, StringComparison.Ordinal);
        Assert.Contains("deploy/container-image", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("publish-image.ps1", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("docker push", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", containerImagePublishing, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("sc.exe create", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("install-service.ps1", windowsServiceDeployment, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("AspNetCoreModuleV2", iisDeployment, StringComparison.Ordinal);
        Assert.Contains("install-site.ps1", iisDeployment, StringComparison.Ordinal);
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
        Assert.Contains("cephalon doctor", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", cliPackageReadme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not-claimed", cliPackageReadme, StringComparison.Ordinal);
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
        Assert.Contains("CompositionSmokeTests.cs", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("BehaviorSpecifications.cs", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("cephalon doctor --app-root ./Acme.Store", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("deployment-mode support contract", templatePackReadme, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("CephalonFolder.pubxml", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-windows-service.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-iis.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-app-service.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-image.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-container-apps.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-kubernetes.ps1", rootReadme, StringComparison.Ordinal);
        Assert.Contains("docker compose -f ./Acme.Store/compose.yaml up --build", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("compose.yaml", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains("docker compose up --build", templatePackReadme, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", gettingStarted, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("NuGet.config", cliPackageReadme, StringComparison.Ordinal);
        Assert.Contains(".cephalon/packages", templatePackReadme, StringComparison.Ordinal);
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
        Assert.Contains("external-package-lifecycle.md", packagePublishing, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", moduleAuthoring, StringComparison.Ordinal);
        Assert.Contains("Engine:PackagePolicy", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("Engine:Trust", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("/engine/packages", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", referenceModuleReadme, StringComparison.Ordinal);
        Assert.Contains("Engine:Discovery", referenceModuleReadme, StringComparison.Ordinal);
        Assert.Contains("external package staging", cliComponentDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cephalon package stage", cliPackageReadme, StringComparison.Ordinal);
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
