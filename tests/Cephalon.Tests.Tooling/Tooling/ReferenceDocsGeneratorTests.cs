using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    public void GenerateBuildsPageForAbstractionsAssemblyWithPhase8Contracts()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-abstractions-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Abstractions"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var abstractionsPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-abstractions.md");

        Assert.Contains("Cephalon.Abstractions.Authorization", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("AuthorizationPolicyDescriptor", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Abstractions.Tenancy", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("TenantContext", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Abstractions.Ids", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("InboxDescriptor", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IInboxCatalog", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("OutboxDescriptor", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IOutboxCatalog", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventDispatchRuntimeDescriptor", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEventDispatchRuntimeCatalog", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Abstractions.EventSourcing", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IDomainEvent", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventStreamConcurrencyException", abstractionsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEventStoreCatalog", abstractionsPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForEngineAssemblyWithPhase8RuntimeSnapshotMembers()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-engine-phase8-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Engine"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var enginePage = Assert.Single(rendered.Files, file => file.Path == "cephalon-engine.md");

        Assert.Contains("RuntimeIntrospectionSnapshot", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("Projections", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("Inboxes", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("Outboxes", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventDispatchRuntimes", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventDispatchStates", enginePage.Contents, StringComparison.Ordinal);
        Assert.Contains("AuthorizationPolicies", enginePage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForSfidIdsAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-sfid-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Ids.Sfid"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var idsPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-ids-sfid.md");

        Assert.Contains("SfidIdOptions", idsPage.Contents, StringComparison.Ordinal);
        Assert.Contains("SfidEngineBuilderExtensions", idsPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForDataAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-data-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Data"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var dataPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-data.md");

        Assert.Contains("DataRuntimeOptions", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("DataEngineBuilderExtensions", dataPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForDataEntityFrameworkAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-data-ef-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Data.EntityFramework"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var dataPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-data-entityframework.md");

        Assert.Contains("EntityFrameworkDataOptions", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkDataEngineBuilderExtensions", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkInboxEntry", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEntityFrameworkInboxContext", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkOutboxEntry", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEntityFrameworkOutboxContext", dataPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EnableSfidIdentifiers", dataPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForEventSourcingAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-event-sourcing-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.EventSourcing"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var eventSourcingPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-eventsourcing.md");

        Assert.Contains("EventSourcingOptions", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventSourcingEngineBuilderExtensions", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventSourcingServiceCollectionExtensions", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("AggregateHydrator", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventStreamCatalog", eventSourcingPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForEventSourcingEntityFrameworkAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-event-sourcing-ef-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.EventSourcing.EntityFramework"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var eventSourcingPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-eventsourcing-entityframework.md");

        Assert.Contains("EntityFrameworkEventEntry", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkEventSourcingConfiguration", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEntityFrameworkEventContext", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkEventSourcingEngineBuilderExtensions", eventSourcingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkEventSourcingServiceCollectionExtensions", eventSourcingPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForEventingAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-eventing-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Eventing"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var eventingPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-eventing.md");

        Assert.Contains("EventPublication", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEventPublisher", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventDispatchItem", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventDispatchExecutionReport", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEventDispatchStore", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEventDispatchRuntimeReporter", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventChannelDescriptor", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventSubscriptionExecutionReport", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IEventSubscriptionRuntimeReporter", eventingPage.Contents, StringComparison.Ordinal);
        Assert.Contains("EventSubscriptionDescriptor", eventingPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForWolverineEventingAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-eventing-wolverine-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Eventing.Wolverine"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var wolverinePage = Assert.Single(rendered.Files, file => file.Path == "cephalon-eventing-wolverine.md");

        Assert.Contains("WolverineEventingOptions", wolverinePage.Contents, StringComparison.Ordinal);
        Assert.Contains("WolverineEventingEngineBuilderExtensions", wolverinePage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForBehaviorEventingBridgeAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-eventing-behaviors-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Eventing.Behaviors"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var bridgePage = Assert.Single(rendered.Files, file => file.Path == "cephalon-eventing-behaviors.md");

        Assert.Contains("BehaviorEventingEngineBuilderExtensions", bridgePage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForIdentityAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-identity-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Identity"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var identityPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-identity.md");

        Assert.Contains("IdentityRuntimeOptions", identityPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IdentityPolicyMetadataKeys", identityPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IdentityEngineBuilderExtensions", identityPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForIdentityAspNetCoreAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-identity-aspnetcore-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Identity.AspNetCore"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var identityPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-identity-aspnetcore.md");

        Assert.Contains("IdentityAspNetCoreOptions", identityPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IdentityAspNetCoreServiceCollectionExtensions", identityPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IdentityEndpointConventionBuilderExtensions", identityPage.Contents, StringComparison.Ordinal);
        Assert.Contains("RequireCephalonAuthorizationAttribute", identityPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForMultiTenancyAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-multi-tenancy-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.MultiTenancy"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var tenancyPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-multitenancy.md");

        Assert.Contains("MultiTenancyRuntimeOptions", tenancyPage.Contents, StringComparison.Ordinal);
        Assert.Contains("MultiTenancyEngineBuilderExtensions", tenancyPage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForMultiTenancyGovernanceAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-multi-tenancy-governance-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.MultiTenancy.Governance"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var governancePage = Assert.Single(rendered.Files, file => file.Path == "cephalon-multitenancy-governance.md");

        Assert.Contains("MultiTenancyGovernanceOptions", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("MultiTenancyGovernanceEngineBuilderExtensions", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("TenantMembershipDescriptor", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("ITenantMembershipEvaluator", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("TenantInvitationDescriptor", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("ITenantInvitationValidator", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("TenantDomainOwnershipDescriptor", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("ITenantDomainOwnershipValidator", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("TenantGovernanceActionDescriptor", governancePage.Contents, StringComparison.Ordinal);
        Assert.Contains("ITenantGovernanceActionDecider", governancePage.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBuildsPageForAuditAssembly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-audit-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: ["Cephalon.Audit"]);

        var rendered = ReferenceDocsGenerator.Generate(request);
        var auditPage = Assert.Single(rendered.Files, file => file.Path == "cephalon-audit.md");

        Assert.Contains("AuditRuntimeOptions", auditPage.Contents, StringComparison.Ordinal);
        Assert.Contains("AuditMetadataKeys", auditPage.Contents, StringComparison.Ordinal);
        Assert.Contains("AuditRecordRequest", auditPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IAuditActorAccessor", auditPage.Contents, StringComparison.Ordinal);
        Assert.Contains("IAuditRecorder", auditPage.Contents, StringComparison.Ordinal);
        Assert.Contains("AuditEngineBuilderExtensions", auditPage.Contents, StringComparison.Ordinal);
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
                "Cephalon.Audit",
                "Cephalon.AspNetCore",
                "Cephalon.AspNetCore.GraphQL",
                "Cephalon.AspNetCore.Grpc",
                "Cephalon.AspNetCore.JsonRpc",
                "Cephalon.Cli",
                "Cephalon.Data",
                "Cephalon.Data.EntityFramework",
                "Cephalon.Edge",
                "Cephalon.Edge.KubernetesGateway",
                "Cephalon.Edge.Traefik",
                "Cephalon.Engine",
                "Cephalon.EventSourcing",
                "Cephalon.EventSourcing.EntityFramework",
                "Cephalon.Eventing",
                "Cephalon.Eventing.Behaviors",
                "Cephalon.Eventing.Wolverine",
                "Cephalon.Identity",
                "Cephalon.Identity.AspNetCore",
                "Cephalon.Ids.Sfid",
                "Cephalon.MultiTenancy",
                "Cephalon.MultiTenancy.Governance",
                "Cephalon.Observability",
                "Cephalon.Observability.CassandraDependencies",
                "Cephalon.Observability.ClickHouseDependencies",
                "Cephalon.Observability.ConsulDependencies",
                "Cephalon.Observability.ElasticsearchDependencies",
                "Cephalon.Observability.HttpDependencies",
                "Cephalon.Observability.KafkaDependencies",
                "Cephalon.Observability.MemcachedDependencies",
                "Cephalon.Observability.MongoDbDependencies",
                "Cephalon.Observability.MqttDependencies",
                "Cephalon.Observability.MySqlDependencies",
                "Cephalon.Observability.NatsDependencies",
                "Cephalon.Observability.Neo4jDependencies",
                "Cephalon.Observability.OpenSearchDependencies",
                "Cephalon.Observability.OracleDependencies",
                "Cephalon.Observability.PostgresDependencies",
                "Cephalon.Observability.RabbitMqDependencies",
                "Cephalon.Observability.RedisDependencies",
                "Cephalon.Observability.SqlServerDependencies",
                "Cephalon.Observability.OracleCloud",
                "Cephalon.Observability.Kubernetes",
                "Cephalon.Observability.GrafanaCloud",
                "Cephalon.Observability.NewRelic",
                "Cephalon.Observability.OpenShift",
                "Cephalon.Observability.Tanzu",
                "Cephalon.Observability.AzureMonitor",
                "Cephalon.Observability.OpenTelemetry",
                "Cephalon.Observability.Serilog",
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
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-audit.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-identity-aspnetcore.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-multitenancy.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-multitenancy-governance.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-data-mysql.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-data-debezium.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-data-oracle.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-eventsourcing.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-eventsourcing-entityframework.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-edge-kubernetesgateway.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-edge-traefik.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-cassandradependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-clickhousedependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-consuldependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-elasticsearchdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-kafkadependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-memcacheddependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-mongodbdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-mqttdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-mysqldependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-natsdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-neo4jdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-opensearchdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-oracledependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-postgresdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-rabbitmqdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-redisdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-sqlserverdependencies.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-oraclecloud.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-kubernetes.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-grafanacloud.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-newrelic.md");
        Assert.Contains(rendered.Files, static file => file.Path == "cephalon-observability-serilog.md");

        var manifest = Assert.Single(rendered.Files, file => file.Path == "reference-manifest.json");
        using var manifestDocument = JsonDocument.Parse(manifest.Contents);
        var assemblies = manifestDocument.RootElement.GetProperty("Assemblies").EnumerateArray().ToArray();

        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Audit", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.AspNetCore.GraphQL", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Identity.AspNetCore", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.MultiTenancy", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.MultiTenancy.Governance", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Data.MySql", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Data.Debezium", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Data.Oracle", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.EventSourcing", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.EventSourcing.EntityFramework", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Edge.KubernetesGateway", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Edge.Traefik", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.CassandraDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.ClickHouseDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.ConsulDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.ElasticsearchDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.KafkaDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.MemcachedDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.MongoDbDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.MqttDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.MySqlDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.NatsDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.Neo4jDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.OpenSearchDependencies", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.OracleDependencies", StringComparison.Ordinal));
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
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.OracleCloud", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.Kubernetes", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.GrafanaCloud", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.NewRelic", StringComparison.Ordinal));
        Assert.Contains(
            assemblies,
            static assembly => string.Equals(assembly.GetProperty("AssemblyName").GetString(), "Cephalon.Observability.Serilog", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateDefaultCatalogMatchesCheckedInReferenceBundle()
    {
        var repositoryRoot = GetRepositoryRoot();
        var checkedInReferenceRoot = Path.Combine(repositoryRoot, "docs", "reference");
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-reference-docs-checked-in-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: repositoryRoot,
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration());

        var rendered = ReferenceDocsGenerator.Generate(request);
        var renderedFiles = rendered.Files
            .ToDictionary(static file => file.Path, static file => file.Contents, StringComparer.Ordinal);
        var checkedInFiles = Directory
            .GetFiles(checkedInReferenceRoot, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(checkedInReferenceRoot, path).Replace('\\', '/'),
                static path => File.ReadAllText(path),
                StringComparer.Ordinal);

        var renderedPaths = renderedFiles.Keys.OrderBy(static path => path, StringComparer.Ordinal).ToArray();
        var checkedInPaths = checkedInFiles.Keys.OrderBy(static path => path, StringComparer.Ordinal).ToArray();

        Assert.True(
            renderedPaths.SequenceEqual(checkedInPaths, StringComparer.Ordinal),
            CreateReferenceBundleDriftMessage(
                "file-set",
                [
                    .. renderedPaths.Except(checkedInPaths, StringComparer.Ordinal).Select(static path => $"+ {path}"),
                    .. checkedInPaths.Except(renderedPaths, StringComparer.Ordinal).Select(static path => $"- {path}")
                ]));

        foreach (var renderedFile in renderedFiles)
        {
            var checkedInContents = checkedInFiles[renderedFile.Key];
            Assert.True(
                string.Equals(
                    NormalizeReferenceDocContents(renderedFile.Key, renderedFile.Value),
                    NormalizeReferenceDocContents(renderedFile.Key, checkedInContents),
                    StringComparison.Ordinal),
                CreateReferenceBundleDriftMessage("content", [$"~ {renderedFile.Key}"]));
        }
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

    private static string NormalizeReferenceDocContents(string path, string contents)
    {
        var normalized = contents
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return path switch
        {
            "reference-manifest.json" => NormalizeReferenceManifestJson(normalized),
            "browse.html" => NormalizeReferenceBrowserHtml(normalized),
            _ => normalized
        };
    }

    private static string NormalizeReferenceManifestJson(string manifestJson)
    {
        var manifest = JsonNode.Parse(manifestJson)?.AsObject()
            ?? throw new InvalidOperationException("Reference manifest could not be parsed.");

        manifest["GeneratedAtUtc"] = "__GENERATED_AT_UTC__";

        return manifest.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string NormalizeReferenceBrowserHtml(string html)
    {
        const string startToken = "<script id=\"reference-manifest\" type=\"application/json\">";
        const string endToken = "</script>";

        var startIndex = html.IndexOf(startToken, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, "Checked-in reference browser HTML is missing the inline manifest script.");

        startIndex += startToken.Length;

        var endIndex = html.IndexOf(endToken, startIndex, StringComparison.Ordinal);
        Assert.True(endIndex >= 0, "Checked-in reference browser HTML is missing the inline manifest terminator.");

        var manifestJson = html[startIndex..endIndex];
        var normalizedManifest = NormalizeReferenceManifestJson(manifestJson);

        return string.Concat(html.AsSpan(0, startIndex), normalizedManifest, html.AsSpan(endIndex));
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

    private static string CreateReferenceBundleDriftMessage(string scope, string[] entries)
    {
        var preview = entries.Length == 0
            ? string.Empty
            : $"{Environment.NewLine}{string.Join(Environment.NewLine, entries)}";

        return $"Checked-in docs/reference bundle drift detected for {scope}. Run `pwsh ./scripts/publish-reference-docs.ps1` and commit the refreshed docs/reference output.{preview}";
    }
}
