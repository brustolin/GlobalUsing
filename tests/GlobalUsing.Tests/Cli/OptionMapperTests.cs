using System.CommandLine;
using GlobalUsing.Cli;

namespace GlobalUsing.Tests.Cli;

public sealed class OptionMapperTests
{
    [Fact]
    public void Map_sets_summary_only_when_flag_is_present()
    {
        var options = new CliOptionSet();
        var command = new Command("report");
        options.AddTo(command);
        var parseResult = command.Parse(["--summary-only"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.True(mapped.SummaryOnly);
    }

    [Fact]
    public void Map_sets_target_namespaces_when_option_is_present_more_than_once()
    {
        var options = new CliOptionSet();
        var command = new Command("report");
        options.AddTo(command);
        var parseResult = command.Parse(["--namespace", " System.Linq ", "--namespace", "System.Text.Json", "--namespace", "System.Linq"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(["System.Linq", "System.Text.Json"], mapped.TargetNamespaces);
    }

    [Fact]
    public void Map_sets_move_namespaces_when_option_is_present_more_than_once()
    {
        var options = new CliOptionSet();
        var command = new Command("apply");
        options.AddTo(command);
        var parseResult = command.Parse(["--move", " System.Linq ", "--move", "System.Text.Json", "--move", "System.Linq"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(["System.Linq", "System.Text.Json"], mapped.MoveNamespaces);
    }

    [Fact]
    public void Map_sets_ignore_namespaces_when_option_is_present_more_than_once()
    {
        var options = new CliOptionSet();
        var command = new Command("apply");
        options.AddTo(command);
        var parseResult = command.Parse(["--ignore", " System.Linq ", "--ignore", "System.Text.Json", "--ignore", "System.Linq"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(["System.Linq", "System.Text.Json"], mapped.IgnoreNamespaces);
    }

    [Fact]
    public void Map_loads_values_from_explicit_config_file()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var configPath = Path.Combine(temporaryDirectory.Path, "custom.json");
        File.WriteAllText(
            configPath,
            """
            {
              "threshold": 91,
              "minFiles": 3,
              "globalFile": "RepoGlobalUsings.cs",
              "format": "markdown",
              "exclude": ["artifacts/**"],
              "move": ["System.Text.Json"],
              "ignore": ["System.Net.Http"],
              "includeStatic": true,
              "includeAlias": true
            }
            """);
        var options = new CliOptionSet();
        var command = new Command("report");
        options.AddTo(command);
        var parseResult = command.Parse(["--config", configPath]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(91, mapped.ThresholdPercentage);
        Assert.Equal(3, mapped.MinFiles);
        Assert.Equal("RepoGlobalUsings.cs", mapped.GlobalUsingsFileName);
        Assert.Equal(GlobalUsing.Core.Enums.ReportFormat.Markdown, mapped.Format);
        Assert.Equal(["artifacts/**"], mapped.ExcludePatterns);
        Assert.Equal(["System.Text.Json"], mapped.MoveNamespaces);
        Assert.Equal(["System.Net.Http"], mapped.IgnoreNamespaces);
        Assert.True(mapped.IncludeStatic);
        Assert.True(mapped.IncludeAlias);
        Assert.Equal(Path.GetFullPath(configPath), mapped.ConfigPath);
    }

    [Fact]
    public void Map_does_not_auto_discover_directory_config_without_explicit_config_argument()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var root = temporaryDirectory.Path;
        var projectDirectory = Path.Combine(root, "src", "App");
        Directory.CreateDirectory(projectDirectory);
        File.WriteAllText(
            Path.Combine(root, "globalusing.json"),
            """
            {
              "threshold": 77,
              "move": ["System.Text.Json"]
            }
            """);
        var options = new CliOptionSet();
        var command = new Command("apply");
        options.AddTo(command);
        var parseResult = command.Parse(["--path", projectDirectory]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(80, mapped.ThresholdPercentage);
        Assert.Empty(mapped.MoveNamespaces);
        Assert.Null(mapped.ConfigPath);
    }

    [Fact]
    public void Map_prefers_cli_values_over_config_values()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var configPath = Path.Combine(temporaryDirectory.Path, "globalusing.json");
        File.WriteAllText(
            configPath,
            """
            {
              "threshold": 77,
              "move": ["System.Text.Json"],
              "ignore": ["System.Net.Http"],
              "includeStatic": false
            }
            """);
        var options = new CliOptionSet();
        var command = new Command("apply");
        options.AddTo(command);
        var parseResult = command.Parse(["--config", configPath, "--threshold", "95", "--move", "System.Linq", "--include-static"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(95, mapped.ThresholdPercentage);
        Assert.Equal(["System.Linq"], mapped.MoveNamespaces);
        Assert.Equal(["System.Net.Http"], mapped.IgnoreNamespaces);
        Assert.True(mapped.IncludeStatic);
    }

    [Fact]
    public void Map_warns_and_ignores_move_when_namespace_and_move_are_set_by_cli()
    {
        var options = new CliOptionSet();
        var command = new Command("apply");
        options.AddTo(command);
        var parseResult = command.Parse(["--namespace", "System.Linq", "--move", "System.Text.Json"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(["System.Linq"], mapped.TargetNamespaces);
        Assert.Empty(mapped.MoveNamespaces);
        Assert.Single(mapped.Warnings);
        Assert.Contains("ignored", mapped.Warnings[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_warns_and_ignores_move_when_namespace_comes_from_config_and_move_from_cli()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var configPath = Path.Combine(temporaryDirectory.Path, "globalusing.json");
        File.WriteAllText(
            configPath,
            """
            {
              "namespace": ["System.Linq"]
            }
            """);
        var options = new CliOptionSet();
        var command = new Command("apply");
        options.AddTo(command);
        var parseResult = command.Parse(["--config", configPath, "--move", "System.Text.Json"]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(["System.Linq"], mapped.TargetNamespaces);
        Assert.Empty(mapped.MoveNamespaces);
        Assert.Single(mapped.Warnings);
    }

    [Fact]
    public void Map_warns_and_ignores_move_when_both_come_from_config()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var configPath = Path.Combine(temporaryDirectory.Path, "globalusing.json");
        File.WriteAllText(
            configPath,
            """
            {
              "namespace": ["System.Linq"],
              "move": ["System.Text.Json"]
            }
            """);
        var options = new CliOptionSet();
        var command = new Command("report");
        options.AddTo(command);
        var parseResult = command.Parse(["--config", configPath]);

        var mapped = OptionMapper.Map(parseResult, options);

        Assert.Equal(["System.Linq"], mapped.TargetNamespaces);
        Assert.Empty(mapped.MoveNamespaces);
        Assert.Single(mapped.Warnings);
    }
}
