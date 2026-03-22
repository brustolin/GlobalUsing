using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlobalUsing.Cli;

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(AnalysisConfigFile))]
internal sealed partial class AnalysisConfigJsonSerializerContext : JsonSerializerContext;
