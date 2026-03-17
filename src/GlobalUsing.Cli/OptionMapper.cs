using System.CommandLine;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Cli;

internal static class OptionMapper
{
    public static AnalysisOptions Map(ParseResult parseResult, CliOptionSet optionSet)
    {
        var formatValue = parseResult.GetValue(optionSet.FormatOption);
        return new AnalysisOptions(
            Path: parseResult.GetValue(optionSet.PathOption) ?? Environment.CurrentDirectory,
            ThresholdPercentage: parseResult.GetValue(optionSet.ThresholdOption) ?? 80,
            MinFiles: parseResult.GetValue(optionSet.MinFilesOption) ?? 1,
            GlobalUsingsFileName: parseResult.GetValue(optionSet.GlobalFileOption) ?? "GlobalUsings.cs",
            Format: ParseFormat(formatValue),
            ExcludePatterns: parseResult.GetValue(optionSet.ExcludeOption) ?? [],
            IncludeStatic: parseResult.GetValue(optionSet.IncludeStaticOption),
            IncludeAlias: parseResult.GetValue(optionSet.IncludeAliasOption),
            SummaryOnly: parseResult.GetValue(optionSet.SummaryOnlyOption),
            DryRun: parseResult.GetValue(optionSet.DryRunOption),
            Verbose: parseResult.GetValue(optionSet.VerboseOption));
    }

    private static ReportFormat ParseFormat(string? formatValue) =>
        formatValue?.ToLowerInvariant() switch
        {
            null or "" or "console" => ReportFormat.Console,
            "json" => ReportFormat.Json,
            "markdown" => ReportFormat.Markdown,
            _ => throw new ArgumentException($"Unsupported format '{formatValue}'. Use console, json, or markdown."),
        };
}
