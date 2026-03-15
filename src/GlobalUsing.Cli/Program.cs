using System.CommandLine;
using GlobalUsing.Analysis;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Infrastructure;
using GlobalUsing.Roslyn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GlobalUsing.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var serviceProvider = BuildServiceProvider(args);
        var reportHandler = serviceProvider.GetRequiredService<ReportCommandHandler>();
        var applyHandler = serviceProvider.GetRequiredService<ApplyCommandHandler>();
        var rootCommand = new RootCommand("Analyze C# using directives and recommend global usings.");

        rootCommand.Subcommands.Add(BuildReportCommand(reportHandler));
        rootCommand.Subcommands.Add(BuildApplyCommand(applyHandler));

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static Command BuildReportCommand(ReportCommandHandler handler)
    {
        var options = new CliOptionSet();
        var command = new Command("report", "Analyze files and produce a report without modifying anything.");
        options.AddTo(command);
        command.SetAction((parseResult, cancellationToken) => handler.InvokeAsync(OptionMapper.Map(parseResult, options), cancellationToken));
        return command;
    }

    private static Command BuildApplyCommand(ApplyCommandHandler handler)
    {
        var options = new CliOptionSet();
        var command = new Command("apply", "Analyze files, update the global usings file, and remove redundant local usings.");
        options.AddTo(command);
        command.SetAction((parseResult, cancellationToken) => handler.InvokeAsync(OptionMapper.Map(parseResult, options), cancellationToken));
        return command;
    }

    private static ServiceProvider BuildServiceProvider(string[] args)
    {
        var services = new ServiceCollection();
        var verbose = args.Any(argument => string.Equals(argument, "--verbose", StringComparison.OrdinalIgnoreCase));

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options => options.SingleLine = true);
            logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        });

        services.AddSingleton<IConsoleWriter, ConsoleWriter>();
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<IFileDiscoveryService, FileDiscoveryService>();
        services.AddSingleton<IReportGenerator, ReportGenerator>();
        services.AddSingleton<IGlobalUsingsWriter, GlobalUsingsWriter>();
        services.AddSingleton<SourceDocumentCache>();
        services.AddSingleton<IUsingCollector, CachingUsingCollector>();
        services.AddSingleton<ISourceFileRewriter, SourceFileRewriter>();
        services.AddSingleton<INamespaceUsageAnalyzer, NamespaceUsageAnalyzer>();
        services.AddSingleton<IGlobalUsingRecommender, GlobalUsingRecommender>();
        services.AddSingleton<IAnalysisWorkflow, AnalysisWorkflow>();
        services.AddTransient<ReportCommandHandler>();
        services.AddTransient<ApplyCommandHandler>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
