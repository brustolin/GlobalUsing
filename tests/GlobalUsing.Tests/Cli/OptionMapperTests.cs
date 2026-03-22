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
}
