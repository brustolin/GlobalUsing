using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface IGlobalUsingRecommender
{
    ProjectAnalysisResult Recommend(
        DiscoveredProject project,
        UsageAnalysisSnapshot snapshot,
        AnalysisOptions options);
}
