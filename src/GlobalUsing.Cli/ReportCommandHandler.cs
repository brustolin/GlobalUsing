using GlobalUsing.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GlobalUsing.Cli;

internal sealed class ReportCommandHandler(
    IAnalysisWorkflow analysisWorkflow,
    IReportGenerator reportGenerator,
    IConsoleWriter consoleWriter,
    ILogger<ReportCommandHandler> logger)
{
    public async Task<int> InvokeAsync(Core.Models.AnalysisOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await analysisWorkflow.ReportAsync(options, cancellationToken);
            consoleWriter.WriteLine(reportGenerator.Generate(result, options.Format, options.SummaryOnly));
            return result.Summary.CandidatesAboveThreshold > 0 ? CliExitCodes.CandidatesDetected : CliExitCodes.Success;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Report command failed.");
            consoleWriter.WriteError(exception.Message);
            return CliExitCodes.ExecutionError;
        }
    }
}
