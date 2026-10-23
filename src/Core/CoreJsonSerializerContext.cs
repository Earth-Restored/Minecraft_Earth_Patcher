using System.Text.Json.Serialization;

namespace MCEPatcher.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PatchInfo))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(Dictionary<string, IEnumerable<string>>))]
[JsonSerializable(typeof(Dictionary<string, IEnumerable<Dictionary<string, string>>>))]
internal sealed partial class CoreJsonSerializerContext : JsonSerializerContext
{
}
