using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlobalUsing.Infrastructure;

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(DirectoryAnalysisConfigFile))]
internal sealed partial class DirectoryAnalysisConfigJsonSerializerContext : JsonSerializerContext;
