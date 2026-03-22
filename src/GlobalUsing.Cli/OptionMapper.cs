using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Cli;

internal static class OptionMapper
{
    public static AnalysisOptions Map(ParseResult parseResult, CliOptionSet optionSet)
    {
        var configPath = ResolveConfigPath(parseResult, optionSet);
        var config = LoadConfig(configPath);
        var targetNamespaces = GetConfiguredList(parseResult, optionSet.NamespaceOption, config?.Namespace);
        var moveNamespaces = GetConfiguredList(parseResult, optionSet.MoveOption, config?.Move);
        var ignoreNamespaces = GetConfiguredList(parseResult, optionSet.IgnoreOption, config?.Ignore);
        var warnings = new List<string>();
        var cliOverrides = new AnalysisOptionOverrides(
            ThresholdPercentage: IsExplicit(parseResult, optionSet.ThresholdOption),
            MinFiles: IsExplicit(parseResult, optionSet.MinFilesOption),
            GlobalUsingsFileName: IsExplicit(parseResult, optionSet.GlobalFileOption),
            Format: IsExplicit(parseResult, optionSet.FormatOption),
            ExcludePatterns: IsExplicit(parseResult, optionSet.ExcludeOption),
            TargetNamespaces: IsExplicit(parseResult, optionSet.NamespaceOption),
            MoveNamespaces: IsExplicit(parseResult, optionSet.MoveOption),
            IgnoreNamespaces: IsExplicit(parseResult, optionSet.IgnoreOption),
            IncludeStatic: IsExplicit(parseResult, optionSet.IncludeStaticOption),
            IncludeAlias: IsExplicit(parseResult, optionSet.IncludeAliasOption));

        if (targetNamespaces.Length > 0 && moveNamespaces.Length > 0)
        {
            warnings.Add("`--move` values are ignored because `--namespace` scopes the run to specific namespaces.");
            moveNamespaces = [];
        }

        return new AnalysisOptions(
            Path: parseResult.GetValue(optionSet.PathOption) ?? Environment.CurrentDirectory,
            ThresholdPercentage: GetConfiguredValue(parseResult, optionSet.ThresholdOption, config?.Threshold) ?? 80,
            MinFiles: GetConfiguredValue(parseResult, optionSet.MinFilesOption, config?.MinFiles) ?? 1,
            GlobalUsingsFileName: GetConfiguredValue(parseResult, optionSet.GlobalFileOption, config?.GlobalFile) ?? "GlobalUsings.cs",
            Format: ParseFormat(GetConfiguredValue(parseResult, optionSet.FormatOption, config?.Format)),
            ExcludePatterns: GetConfiguredList(parseResult, optionSet.ExcludeOption, config?.Exclude),
            ConfigPath: configPath,
            TargetNamespaces: targetNamespaces,
            MoveNamespaces: moveNamespaces,
            IgnoreNamespaces: ignoreNamespaces,
            CliOverrides: cliOverrides,
            Warnings: warnings,
            IncludeStatic: GetConfiguredFlag(parseResult, optionSet.IncludeStaticOption, config?.IncludeStatic),
            IncludeAlias: GetConfiguredFlag(parseResult, optionSet.IncludeAliasOption, config?.IncludeAlias),
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

    private static string[] NormalizeNamespaces(string[]? namespaceValues) =>
        namespaceValues is null
            ? []
            : namespaceValues
                .Select(namespaceValue => namespaceValue?.Trim())
                .OfType<string>()
                .Where(namespaceValue => namespaceValue.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

    private static T? GetConfiguredValue<T>(ParseResult parseResult, Option<T?> option, T? configValue)
    {
        var result = parseResult.GetResult(option);
        return result is OptionResult { Implicit: false }
            ? parseResult.GetValue(option)
            : configValue;
    }

    private static bool GetConfiguredFlag(ParseResult parseResult, Option<bool> option, bool? configValue)
    {
        var result = parseResult.GetResult(option);
        return result is OptionResult { Implicit: false }
            ? parseResult.GetValue(option)
            : configValue ?? false;
    }

    private static string[] GetConfiguredList(ParseResult parseResult, Option<string[]> option, string[]? configValues)
    {
        var result = parseResult.GetResult(option);
        return result is OptionResult { Implicit: false }
            ? NormalizeNamespaces(parseResult.GetValue(option))
            : NormalizeNamespaces(configValues);
    }

    private static string? ResolveConfigPath(ParseResult parseResult, CliOptionSet optionSet)
    {
        var explicitConfigPath = parseResult.GetValue(optionSet.ConfigOption);
        if (!string.IsNullOrWhiteSpace(explicitConfigPath))
        {
            var resolvedConfigPath = Path.GetFullPath(explicitConfigPath);
            if (!File.Exists(resolvedConfigPath))
            {
                throw new FileNotFoundException($"Could not resolve config path '{resolvedConfigPath}'.", resolvedConfigPath);
            }

            return resolvedConfigPath;
        }

        return null;
    }

    private static AnalysisConfigFile? LoadConfig(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return null;
        }

        try
        {
            var content = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize(content, AnalysisConfigJsonSerializerContext.Default.AnalysisConfigFile)
                ?? new AnalysisConfigFile(null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Failed to parse config file '{configPath}': {exception.Message}", exception);
        }
    }

    private static bool IsExplicit<T>(ParseResult parseResult, Option<T> option) =>
        parseResult.GetResult(option) is OptionResult { Implicit: false };
}
