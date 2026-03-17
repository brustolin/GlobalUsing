using System.CommandLine;

namespace GlobalUsing.Cli;

internal sealed class CliOptionSet
{
    public Option<string?> PathOption { get; } = CreateOption<string?>("--path", "Path to a .csproj, .sln, .slnx, or directory to analyze.");

    public Option<int?> ThresholdOption { get; } = CreateOption<int?>("--threshold", "Minimum percentage of files containing a namespace before promotion.");

    public Option<int?> MinFilesOption { get; } = CreateOption<int?>("--min-files", "Minimum number of files that must contain a namespace before promotion.");

    public Option<string?> GlobalFileOption { get; } = CreateOption<string?>("--global-file", "Name of the file containing global usings.");

    public Option<string?> FormatOption { get; } = CreateOption<string?>("--format", "Output format: console, json, or markdown.");

    public Option<string[]> ExcludeOption { get; } = CreateExcludeOption();

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
        command.Options.Add(ExcludeOption);
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
        var option = new Option<string[]>("--exclude")
        {
            Description = "Glob pattern to exclude files or directories.",
            AllowMultipleArgumentsPerToken = false,
        };

        return option;
    }
}
