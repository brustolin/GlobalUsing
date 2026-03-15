using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Xml.Linq;
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
            ".sln" => await DiscoverSolutionAsync(targetPath, options.ExcludePatterns, cancellationToken),
            ".slnx" => await DiscoverSolutionFilterAsync(targetPath, options.ExcludePatterns, cancellationToken),
            ".csproj" => [await DiscoverProjectAsync(targetPath, options.ExcludePatterns, cancellationToken)],
            _ when fileSystem.DirectoryExists(targetPath) => await DiscoverDirectoryAsync(targetPath, options.ExcludePatterns, cancellationToken),
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
        IReadOnlyList<string> excludePatterns,
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
            discoveredProjects.Add(await DiscoverProjectAsync(projectPath, excludePatterns, cancellationToken));
        }

        return discoveredProjects;
    }

    private async Task<IReadOnlyList<DiscoveredProject>> DiscoverSolutionFilterAsync(
        string solutionPath,
        IReadOnlyList<string> excludePatterns,
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
            discoveredProjects.Add(await DiscoverProjectAsync(projectPath, excludePatterns, cancellationToken));
        }

        return discoveredProjects;
    }

    private async Task<IReadOnlyList<DiscoveredProject>> DiscoverDirectoryAsync(
        string directoryPath,
        IReadOnlyList<string> excludePatterns,
        CancellationToken cancellationToken)
    {
        var projectFiles = EnumerateFiles(directoryPath, "**/*.csproj", excludePatterns);
        if (projectFiles.Length == 0)
        {
            return [new DiscoveredProject(directoryPath, null, false, FrozenSet.ToFrozenSet<string>([], StringComparer.Ordinal), EnumerateFiles(directoryPath, "**/*.cs", excludePatterns))];
        }

        var projects = new List<DiscoveredProject>(projectFiles.Length);

        foreach (var projectFile in projectFiles)
        {
            projects.Add(await DiscoverProjectAsync(projectFile, excludePatterns, cancellationToken));
        }

        return projects;
    }

    private async Task<DiscoveredProject> DiscoverProjectAsync(
        string projectPath,
        IReadOnlyList<string> excludePatterns,
        CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path '{projectPath}' does not have a parent directory.");
        var projectContent = await fileSystem.ReadAllTextAsync(projectPath, cancellationToken);
        var document = XDocument.Parse(projectContent, LoadOptions.PreserveWhitespace);
        var implicitUsingsEnabled = IsImplicitUsingsEnabled(document);
        var sdkName = GetSdkName(document);
        var namespaces = implicitUsingsEnabled
            ? ImplicitUsingNamespaces.ForSdk(sdkName)
            : FrozenSet.ToFrozenSet<string>([], StringComparer.Ordinal);
        var files = EnumerateFiles(projectDirectory, "**/*.cs", excludePatterns);

        return new DiscoveredProject(projectDirectory, projectPath, implicitUsingsEnabled, namespaces, files);
    }

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
