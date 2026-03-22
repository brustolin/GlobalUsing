using System.Collections.Immutable;
using GlobalUsing.Cli;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace GlobalUsing.Tests.Cli;

public sealed class ReportCommandHandlerTests
{
    [Fact]
    public async Task InvokeAsync_writes_warning_messages_before_report_output()
    {
        var consoleWriter = new FakeConsoleWriter();
        var handler = new ReportCommandHandler(
            new FakeAnalysisWorkflow(),
            new FakeReportGenerator(),
            consoleWriter,
            NullLogger<ReportCommandHandler>.Instance);
        var options = AnalysisOptions.Default() with
        {
            Warnings = ["`--move` values are ignored because `--namespace` scopes the run to specific namespaces."],
        };

        await handler.InvokeAsync(options, CancellationToken.None);

        Assert.Contains(consoleWriter.Errors, line => line.Contains("Warning:", StringComparison.Ordinal));
        Assert.Contains(consoleWriter.Errors, line => line.Contains("--move", StringComparison.Ordinal));
        Assert.Contains(consoleWriter.Output, line => line == "report");
    }

    private sealed class FakeAnalysisWorkflow : IAnalysisWorkflow
    {
        public Task<AnalysisResult> ReportAsync(AnalysisOptions options, CancellationToken cancellationToken) =>
            Task.FromResult(new AnalysisResult(ImmutableArray<ProjectAnalysisResult>.Empty, new AnalysisSummary(0, 0, 0, 0, 0)));

        public Task<ApplyResult> ApplyAsync(AnalysisOptions options, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeReportGenerator : IReportGenerator
    {
        public string Generate(AnalysisResult result, ReportFormat format, bool summaryOnly = false, IReadOnlyList<string>? targetNamespaces = null) => "report";
    }

    private sealed class FakeConsoleWriter : IConsoleWriter
    {
        public List<string> Output { get; } = [];

        public List<string> Errors { get; } = [];

        public void WriteLine(string value) => Output.Add(value);

        public void WriteError(string value) => Errors.Add(value);
    }
}
