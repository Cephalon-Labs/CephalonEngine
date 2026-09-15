using Cephalon.AspNetCore.Transports;
using Cephalon.AspNetCore.Resilience;
using Cephalon.AspNetCore.Localization;
using Cephalon.AspNetCore.Authorization;
using Cephalon.AspNetCore.Audit;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Diagnostics;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Health;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Trust;

namespace Cephalon.AspNetCore;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HealthResponsePayload))]
[JsonSerializable(typeof(ResultModelError))]
[JsonSerializable(typeof(AuditHistoryEntry))]
[JsonSerializable(typeof(StranglerFigUnsupportedEndpointProblem))]
[JsonSerializable(typeof(RateLimitRejectionProblem))]
[JsonSerializable(typeof(JsonRpcRateLimitRejectionEnvelope))]
[JsonSerializable(typeof(CdcCaptureRuntimeObservation[]))]
[JsonSerializable(typeof(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest))]
[JsonSerializable(typeof(EventPublicationHttpRequest))]
[JsonSerializable(typeof(EventDispatchRemediationHttpRequest))]
[JsonSerializable(typeof(EventDispatchRemediationCommandPage))]
[JsonSerializable(typeof(EventDispatchRemediationRuntimeState))]
[JsonSerializable(typeof(EventDispatchRemediationRuntimeState[]))]
[JsonSerializable(typeof(AgentToolExecutionHttpRequest))]
[JsonSerializable(typeof(KnowledgeQueryHttpRequest))]
[JsonSerializable(typeof(RuntimeManifest))]
[JsonSerializable(typeof(RuntimeIntrospectionSnapshot))]
[JsonSerializable(typeof(AppProfile))]
[JsonSerializable(typeof(ResilienceSelection))]
[JsonSerializable(typeof(ScaffoldPlan))]
[JsonSerializable(typeof(IReadOnlyList<CapabilityManifest>))]
[JsonSerializable(typeof(IReadOnlyList<ModuleManifest>))]
[JsonSerializable(typeof(IReadOnlyList<PackageManifest>))]
[JsonSerializable(typeof(IReadOnlyList<PatternDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<TechnologyDescriptor>))]
[JsonSerializable(typeof(IReadOnlyList<TechnologyRuntimeSurface>))]
[JsonSerializable(typeof(IReadOnlyList<TransportDescriptor>))]
[JsonSerializable(typeof(DependencyHealthReport[]))]
[JsonSerializable(typeof(LocalizedResourcesSnapshot))]
[JsonSerializable(typeof(ReferenceDocsSurface))]
[JsonSerializable(typeof(EngineOptions))]
[JsonSerializable(typeof(PackagePolicy))]
[JsonSerializable(typeof(FailurePolicy))]
[JsonSerializable(typeof(TrustSnapshot))]
[JsonSerializable(typeof(RuntimeStatusSnapshot))]
[JsonSerializable(typeof(RuntimeOperationalStory))]
[JsonSerializable(typeof(RateLimitingRuntimeSurface))]
[JsonSerializable(typeof(DataProductRuntimeSurface))]
[JsonSerializable(typeof(OutboxRuntimeSurface))]
[JsonSerializable(typeof(InboxRuntimeSurface))]
[JsonSerializable(typeof(AuditStoreRuntimeSurface))]
[JsonSerializable(typeof(AuthorizationPolicyRuntimeSurface))]
[JsonSerializable(typeof(TransportRuntimeSurface))]
[JsonSerializable(typeof(LocalizedResourcesRuntimeSurface))]
[JsonSerializable(typeof(ReferenceDocsRuntimeSurface))]
[JsonSerializable(typeof(DiagnosticsSurface))]
[JsonSerializable(typeof(DiagnosticsConventionsSurface))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class AspNetCoreJsonSerializerContext : JsonSerializerContext;
