using System.CommandLine;

namespace GlobalUsing.Cli;

internal sealed class CliOptionSet
{
    public Option<string?> PathOption { get; } = CreateOption<string?>("--path", "Path to a .csproj, .sln, .slnx, or directory to analyze.");

    public Option<int?> ThresholdOption { get; } = CreateOption<int?>("--threshold", "Minimum percentage of files containing a namespace before promotion.");

    public Option<int?> MinFilesOption { get; } = CreateOption<int?>("--min-files", "Minimum number of files that must contain a namespace before promotion.");

    public Option<string?> GlobalFileOption { get; } = CreateOption<string?>("--global-file", "Name of the file containing global usings.");

    public Option<string?> FormatOption { get; } = CreateOption<string?>("--format", "Output format: console, json, or markdown.");

    public Option<string?> ConfigOption { get; } = CreateOption<string?>("--config", "Path to a globalusing.json configuration file.");

    public Option<string[]> ExcludeOption { get; } = CreateExcludeOption();

    public Option<string[]> NamespaceOption { get; } = CreateRepeatableStringOption("--namespace", "Namespace to focus the report on or force into global usings during apply. Repeat the option to target more than one namespace.");

    public Option<string[]> MoveOption { get; } = CreateRepeatableStringOption("--move", "Namespace to force into global usings while still processing all other namespaces normally. Repeat the option to move more than one namespace.");

    public Option<string[]> IgnoreOption { get; } = CreateRepeatableStringOption("--ignore", "Namespace to keep local and never promote to global usings. Repeat the option to ignore more than one namespace.");

    public Option<bool> IncludeStaticOption { get; } = CreateOption<bool>("--include-static", "Include using static directives in analysis.");

    public Option<bool> IncludeAliasOption { get; } = CreateOption<bool>("--include-alias", "Include alias using directives in analysis.");

    public Option<bool> SummaryOnlyOption { get; } = CreateOption<bool>("--summary-only", "Display only the summary section of the report output.");

    public Option<bool> DryRunOption { get; } = CreateOption<bool>("--dry-run", "Show planned changes without writing files.");

    public Option<bool> VerboseOption { get; } = CreateOption<bool>("--verbose", "Enable verbose logging.");

    public void AddTo(Command command)
    {
        command.Options.Add(PathOption);
        command.Options.Add(ThresholdOption);
        command.Options.Add(MinFilesOption);
        command.Options.Add(GlobalFileOption);
        command.Options.Add(FormatOption);
        command.Options.Add(ConfigOption);
        command.Options.Add(ExcludeOption);
        command.Options.Add(NamespaceOption);
        command.Options.Add(MoveOption);
        command.Options.Add(IgnoreOption);
        command.Options.Add(IncludeStaticOption);
        command.Options.Add(IncludeAliasOption);
        command.Options.Add(SummaryOnlyOption);
        command.Options.Add(DryRunOption);
        command.Options.Add(VerboseOption);
    }

    private static Option<T> CreateOption<T>(string name, string description)
    {
        var option = new Option<T>(name);
        option.Description = description;
        return option;
    }

    private static Option<string[]> CreateExcludeOption()
    {
        return CreateRepeatableStringOption("--exclude", "Glob pattern to exclude files or directories.");
    }

    private static Option<string[]> CreateRepeatableStringOption(string name, string description) =>
        new(name)
        {
            Description = description,
            AllowMultipleArgumentsPerToken = false,
        };
}
