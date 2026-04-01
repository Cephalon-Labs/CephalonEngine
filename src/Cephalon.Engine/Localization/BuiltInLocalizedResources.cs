namespace Cephalon.Engine.Localization;

internal static class BuiltInLocalizedResources
{
    public const string DefaultCulture = "en";

    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Resources =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["engine.docs.rest.title"] = "Cephalon REST API",
                ["engine.docs.rest.description"] = "REST surface exposed by the Cephalon ASP.NET Core host.",
                ["engine.docs.scalar.title"] = "Cephalon REST API",
                ["engine.endpoint.localization.title"] = "Cephalon Localization",
                ["engine.endpoint.localization.description"] = "Resolved language resources exposed by the Cephalon runtime.",
                ["blueprint.modular-monolith.displayName"] = "Modular Monolith",
                ["blueprint.modular-vertical-slice.displayName"] = "Modular Vertical Slice",
                ["blueprint.microservice.displayName"] = "Microservice",
                ["transport.rest-api.displayName"] = "REST API",
                ["transport.json-rpc.displayName"] = "JSON-RPC",
                ["transport.grpc.displayName"] = "gRPC",
                ["transport.server-sent-events.displayName"] = "Server-Sent Events",
                ["transport.websocket.displayName"] = "WebSocket"
            },
            ["th"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["engine.docs.rest.title"] = "Cephalon REST API ภาษาไทย",
                ["engine.docs.rest.description"] = "พื้นผิว REST ที่ Cephalon ASP.NET Core host เปิดให้ใช้งาน",
                ["engine.docs.scalar.title"] = "เอกสาร REST API ของ Cephalon",
                ["engine.endpoint.localization.title"] = "ภาษาของ Cephalon",
                ["engine.endpoint.localization.description"] = "ทรัพยากรภาษาที่ runtime ของ Cephalon resolve แล้ว",
                ["blueprint.modular-monolith.displayName"] = "โมดูลาร์โมโนลิธ",
                ["blueprint.modular-vertical-slice.displayName"] = "โมดูลาร์เวอร์ติคัลสไลซ์",
                ["blueprint.microservice.displayName"] = "ไมโครเซอร์วิส",
                ["transport.rest-api.displayName"] = "REST API",
                ["transport.json-rpc.displayName"] = "JSON-RPC",
                ["transport.grpc.displayName"] = "gRPC",
                ["transport.server-sent-events.displayName"] = "Server-Sent Events",
                ["transport.websocket.displayName"] = "WebSocket"
            }
        };

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> All => Resources;
}
