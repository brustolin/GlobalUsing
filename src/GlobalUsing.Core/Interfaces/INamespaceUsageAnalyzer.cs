using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface INamespaceUsageAnalyzer
{
    UsageAnalysisSnapshot Analyze(
        DiscoveredProject project,
        IReadOnlyCollection<SourceFileUsings> sourceFiles,
        AnalysisOptions options);
}
