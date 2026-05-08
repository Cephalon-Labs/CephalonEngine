using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.Engine.Composition.Packages;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(PackageDefinitionFile))]
internal sealed partial class PackageDefinitionFileJsonContext : JsonSerializerContext;
