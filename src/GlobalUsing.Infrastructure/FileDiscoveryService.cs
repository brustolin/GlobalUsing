using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Xml.Linq;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;

namespace GlobalUsing.Infrastructure;

public sealed class FileDiscoveryService(
    IFileSystem fileSystem,
    ILogger<FileDiscoveryService> logger) : IFileDiscoveryService
{
    private const string ConfigFileName = "globalusing.json";

    private static readonly string[] DefaultExcludes =
    [
        "bin/**",
        "obj/**",
        "**/bin/**",
        "**/obj/**",
    ];

    public async Task<FileDiscoveryResult> DiscoverAsync(AnalysisOptions options, CancellationToken cancellationToken)
    {
        var targetPath = Path.GetFullPath(string.IsNullOrWhiteSpace(options.Path) ? Environment.CurrentDirectory : options.Path);
        var projects = Path.GetExtension(targetPath).ToLowerInvariant() switch
        {
            ".sln" => await DiscoverSolutionAsync(targetPath, options, cancellationToken),
            ".slnx" => await DiscoverSolutionFilterAsync(targetPath, options, cancellationToken),
            ".csproj" => [await DiscoverProjectAsync(targetPath, options, cancellationToken)],
            _ when fileSystem.DirectoryExists(targetPath) => await DiscoverDirectoryAsync(targetPath, options, cancellationToken),
            _ => throw new FileNotFoundException($"Could not resolve analysis path '{targetPath}'.", targetPath),
        };

        var materializedProjects = projects
            .Where(project => project.CSharpFiles.Length > 0)
            .DistinctBy(project => project.RootPath, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        logger.LogDebug("Discovered {ProjectCount} projects and {FileCount} C# files.", materializedProjects.Length, materializedProjects.Sum(project => project.CSharpFiles.Length));

        return new FileDiscoveryResult(
            materializedProjects,
            materializedProjects.SelectMany(project => project.CSharpFiles).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToImmutableArray());
    }

    private async Task<IReadOnlyList<DiscoveredProject>> DiscoverSolutionAsync(
        string solutionPath,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var content = await fileSystem.ReadAllTextAsync(solutionPath, cancellationToken);
        var projectPaths = content
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Contains(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(line => ExtractProjectPath(line, solutionDirectory))
            .Where(path => path is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        var discoveredProjects = new List<DiscoveredProject>(projectPaths.Length);

        foreach (var projectPath in projectPaths)
        {
            discoveredProjects.Add(await DiscoverProjectAsync(projectPath, options, cancellationToken));
        }

        return discoveredProjects;
    }

    private async Task<IReadOnlyList<DiscoveredProject>> DiscoverSolutionFilterAsync(
        string solutionPath,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var content = await fileSystem.ReadAllTextAsync(solutionPath, cancellationToken);
        var document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        var projectPaths = document.Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path!)))
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        var discoveredProjects = new List<DiscoveredProject>(projectPaths.Length);

        foreach (var projectPath in projectPaths)
        {
            discoveredProjects.Add(await DiscoverProjectAsync(projectPath, options, cancellationToken));
        }

        return discoveredProjects;
    }

    private async Task<IReadOnlyList<DiscoveredProject>> DiscoverDirectoryAsync(
        string directoryPath,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var projectFiles = EnumerateFiles(directoryPath, "**/*.csproj", options.ExcludePatterns);
        if (projectFiles.Length == 0)
        {
            var effectiveOptions = await ResolveEffectiveOptionsAsync(directoryPath, options, cancellationToken);
            return
            [
                new DiscoveredProject(
                    directoryPath,
                    null,
                    false,
                    FrozenSet.ToFrozenSet<string>([], StringComparer.Ordinal),
                    EnumerateFiles(directoryPath, "**/*.cs", effectiveOptions.ExcludePatterns),
                    effectiveOptions)
            ];
        }

        var projects = new List<DiscoveredProject>(projectFiles.Length);

        foreach (var projectFile in projectFiles)
        {
            projects.Add(await DiscoverProjectAsync(projectFile, options, cancellationToken));
        }

        return projects;
    }

    private async Task<DiscoveredProject> DiscoverProjectAsync(
        string projectPath,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path '{projectPath}' does not have a parent directory.");
        var effectiveOptions = await ResolveEffectiveOptionsAsync(projectDirectory, options, cancellationToken);
        var projectContent = await fileSystem.ReadAllTextAsync(projectPath, cancellationToken);
        var document = XDocument.Parse(projectContent, LoadOptions.PreserveWhitespace);
        var implicitUsingsEnabled = IsImplicitUsingsEnabled(document);
        var sdkName = GetSdkName(document);
        var namespaces = implicitUsingsEnabled
            ? ImplicitUsingNamespaces.ForSdk(sdkName)
            : FrozenSet.ToFrozenSet<string>([], StringComparer.Ordinal);
        var files = EnumerateFiles(projectDirectory, "**/*.cs", effectiveOptions.ExcludePatterns);

        return new DiscoveredProject(projectDirectory, projectPath, implicitUsingsEnabled, namespaces, files, effectiveOptions);
    }

    private async Task<AnalysisOptions> ResolveEffectiveOptionsAsync(
        string directoryPath,
        AnalysisOptions baseOptions,
        CancellationToken cancellationToken)
    {
        var currentOptions = baseOptions;
        var directories = GetAncestorDirectories(directoryPath);

        foreach (var currentDirectory in directories)
        {
            var configPath = Path.Combine(currentDirectory, ConfigFileName);
            if (string.Equals(configPath, baseOptions.ConfigPath, StringComparison.OrdinalIgnoreCase) || !fileSystem.FileExists(configPath))
            {
                continue;
            }

            currentOptions = await ApplyDirectoryConfigAsync(currentOptions, configPath, cancellationToken);
        }

        return currentOptions;
    }

    private async Task<AnalysisOptions> ApplyDirectoryConfigAsync(
        AnalysisOptions currentOptions,
        string configPath,
        CancellationToken cancellationToken)
    {
        DirectoryAnalysisConfigFile? config;

        try
        {
            var content = await fileSystem.ReadAllTextAsync(configPath, cancellationToken);
            config = JsonSerializer.Deserialize(content, DirectoryAnalysisConfigJsonSerializerContext.Default.DirectoryAnalysisConfigFile)
                ?? new DirectoryAnalysisConfigFile(null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Failed to parse config file '{configPath}': {exception.Message}", exception);
        }

        var mergedOptions = currentOptions with
        {
            ThresholdPercentage = currentOptions.CliOverrides.ThresholdPercentage ? currentOptions.ThresholdPercentage : config.Threshold ?? currentOptions.ThresholdPercentage,
            MinFiles = currentOptions.CliOverrides.MinFiles ? currentOptions.MinFiles : config.MinFiles ?? currentOptions.MinFiles,
            GlobalUsingsFileName = currentOptions.CliOverrides.GlobalUsingsFileName ? currentOptions.GlobalUsingsFileName : config.GlobalFile ?? currentOptions.GlobalUsingsFileName,
            Format = currentOptions.CliOverrides.Format ? currentOptions.Format : ParseFormat(config.Format) ?? currentOptions.Format,
            ExcludePatterns = currentOptions.CliOverrides.ExcludePatterns ? currentOptions.ExcludePatterns : config.Exclude is null ? currentOptions.ExcludePatterns : NormalizeNamespaces(config.Exclude),
            TargetNamespaces = currentOptions.CliOverrides.TargetNamespaces ? currentOptions.TargetNamespaces : config.Namespace is null ? currentOptions.TargetNamespaces : NormalizeNamespaces(config.Namespace),
            MoveNamespaces = currentOptions.CliOverrides.MoveNamespaces ? currentOptions.MoveNamespaces : config.Move is null ? currentOptions.MoveNamespaces : NormalizeNamespaces(config.Move),
            IgnoreNamespaces = currentOptions.CliOverrides.IgnoreNamespaces ? currentOptions.IgnoreNamespaces : config.Ignore is null ? currentOptions.IgnoreNamespaces : NormalizeNamespaces(config.Ignore),
            IncludeStatic = currentOptions.CliOverrides.IncludeStatic ? currentOptions.IncludeStatic : config.IncludeStatic ?? currentOptions.IncludeStatic,
            IncludeAlias = currentOptions.CliOverrides.IncludeAlias ? currentOptions.IncludeAlias : config.IncludeAlias ?? currentOptions.IncludeAlias,
        };

        if (mergedOptions.TargetNamespaces.Count > 0 && mergedOptions.MoveNamespaces.Count > 0)
        {
            logger.LogWarning("`--move` values are ignored because `--namespace` scopes the run to specific namespaces.");
            mergedOptions = mergedOptions with { MoveNamespaces = [] };
        }

        return mergedOptions;
    }

    private static ImmutableArray<string> GetAncestorDirectories(string directoryPath)
    {
        var directories = new Stack<string>();
        var currentDirectory = new DirectoryInfo(Path.GetFullPath(directoryPath));

        while (currentDirectory is not null)
        {
            directories.Push(currentDirectory.FullName);
            currentDirectory = currentDirectory.Parent;
        }

        return directories.ToImmutableArray();
    }

    private static string[] NormalizeNamespaces(string[]? namespaceValues) =>
        namespaceValues is null
            ? []
            : namespaceValues
                .Select(namespaceValue => namespaceValue?.Trim())
                .OfType<string>()
                .Where(namespaceValue => namespaceValue.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

    private static ReportFormat? ParseFormat(string? formatValue) =>
        formatValue?.ToLowerInvariant() switch
        {
            null or "" => null,
            "console" => ReportFormat.Console,
            "json" => ReportFormat.Json,
            "markdown" => ReportFormat.Markdown,
            _ => throw new ArgumentException($"Unsupported format '{formatValue}'. Use console, json, or markdown."),
        };

    private static bool IsImplicitUsingsEnabled(XDocument document)
    {
        var value = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "ImplicitUsings")?.Value;
        return string.Equals(value, "enable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetSdkName(XDocument document)
    {
        var projectElement = document.Root;
        var directSdk = projectElement?.Attribute("Sdk")?.Value;
        if (!string.IsNullOrWhiteSpace(directSdk))
        {
            return directSdk;
        }

        return document.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Sdk")
            ?.Attribute("Name")
            ?.Value;
    }

    private static string? ExtractProjectPath(string line, string solutionDirectory)
    {
        var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
        var projectPath = parts.FirstOrDefault(part => part.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        return projectPath is null ? null : Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));
    }

    private static ImmutableArray<string> EnumerateFiles(string rootPath, string includePattern, IReadOnlyList<string> excludePatterns)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(includePattern);

        foreach (var exclude in DefaultExcludes.Concat(excludePatterns).Where(pattern => !string.IsNullOrWhiteSpace(pattern)))
        {
            matcher.AddExclude(NormalizePattern(exclude));
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(rootPath)));
        return result.Files
            .Select(match => Path.GetFullPath(Path.Combine(rootPath, match.Path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    private static string NormalizePattern(string pattern) => pattern.Replace('\\', '/');
}
