using System.Text.Json.Serialization;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Infrastructure;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AnalysisResult))]
[JsonSerializable(typeof(AnalysisSummary))]
[JsonSerializable(typeof(ApplyResult))]
[JsonSerializable(typeof(NamespaceReportSummary))]
[JsonSerializable(typeof(NamespaceReportSummary[]))]
internal sealed partial class InfrastructureJsonSerializerContext : JsonSerializerContext;
