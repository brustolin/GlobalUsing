using GlobalUsing.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GlobalUsing.Cli;

internal sealed class ApplyCommandHandler(
    IAnalysisWorkflow analysisWorkflow,
    IReportGenerator reportGenerator,
    IConsoleWriter consoleWriter,
    ILogger<ApplyCommandHandler> logger)
{
    public async Task<int> InvokeAsync(GlobalUsing.Core.Models.AnalysisOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await analysisWorkflow.ApplyAsync(options, cancellationToken);
            if (options.Format != GlobalUsing.Core.Enums.ReportFormat.Console)
            {
                consoleWriter.WriteLine(reportGenerator.Generate(result.AnalysisResult, options.Format));
                consoleWriter.WriteLine(string.Empty);
            }

            consoleWriter.WriteLine($"Dry run: {result.DryRun}");
            consoleWriter.WriteLine($"Global usings added: {result.GlobalUsingsAdded}");
            consoleWriter.WriteLine($"Files modified: {result.FilesModified}");
            consoleWriter.WriteLine($"Local usings removed: {result.LocalUsingsRemoved}");

            foreach (var fileChange in result.FileChanges.OrderBy(change => change.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                consoleWriter.WriteLine($"{(result.DryRun ? "Plan" : "Updated")}: {fileChange.FilePath} (+{fileChange.AddedGlobalUsings} global, -{fileChange.RemovedLocalUsings} local)");
            }

            return CliExitCodes.Success;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Apply command failed.");
            consoleWriter.WriteError(exception.Message);
            return CliExitCodes.ExecutionError;
        }
    }
}
