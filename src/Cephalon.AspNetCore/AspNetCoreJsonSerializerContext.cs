using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Audit;
using Cephalon.AspNetCore.Health;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;

namespace Cephalon.AspNetCore;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HealthResponsePayload))]
[JsonSerializable(typeof(ResultModelError))]
[JsonSerializable(typeof(AuditHistoryEntry))]
[JsonSerializable(typeof(StranglerFigUnsupportedEndpointProblem))]
[JsonSerializable(typeof(RateLimitRejectionProblem))]
[JsonSerializable(typeof(EventPublicationHttpRequest))]
[JsonSerializable(typeof(AgentToolExecutionHttpRequest))]
[JsonSerializable(typeof(KnowledgeQueryHttpRequest))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class AspNetCoreJsonSerializerContext : JsonSerializerContext;
