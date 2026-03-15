using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface IAnalysisWorkflow
{
    Task<AnalysisResult> ReportAsync(AnalysisOptions options, CancellationToken cancellationToken);

    Task<ApplyResult> ApplyAsync(AnalysisOptions options, CancellationToken cancellationToken);
}
