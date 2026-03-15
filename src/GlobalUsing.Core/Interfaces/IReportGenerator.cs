using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface IReportGenerator
{
    string Generate(AnalysisResult result, ReportFormat format);
}
