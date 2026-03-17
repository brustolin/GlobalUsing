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
}
