using GlobalUsing.Core.Models;
using GlobalUsing.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace GlobalUsing.Tests.Infrastructure;

public sealed class FileDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAsync_respects_default_and_custom_exclusions()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var root = temporaryDirectory.Path;
        Directory.CreateDirectory(Path.Combine(root, "Features"));
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        await File.WriteAllTextAsync(Path.Combine(root, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(root, "Program.cs"), "using System.Linq;");
        await File.WriteAllTextAsync(Path.Combine(root, "Features", "Ignored.cs"), "using Demo;");
        await File.WriteAllTextAsync(Path.Combine(root, "obj", "Generated.cs"), "using System;");

        var service = new FileDiscoveryService(new PhysicalFileSystem(), NullLogger<FileDiscoveryService>.Instance);
        var options = AnalysisOptions.Default(root) with { ExcludePatterns = ["Features/**"] };

        var result = await service.DiscoverAsync(options, CancellationToken.None);

        var discoveredFile = Assert.Single(result.AllFiles);
        Assert.Equal(Path.Combine(root, "Program.cs"), discoveredFile);
        Assert.Single(result.Projects);
        Assert.True(result.Projects[0].ImplicitUsingsEnabled);
    }

    [Fact]
    public async Task DiscoverAsync_supports_slnx_files()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var root = temporaryDirectory.Path;
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "App", "App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "Program.cs"), "using System.Linq;");
        await File.WriteAllTextAsync(
            Path.Combine(root, "App.slnx"),
            """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/App/App.csproj" />
              </Folder>
            </Solution>
            """);

        var service = new FileDiscoveryService(new PhysicalFileSystem(), NullLogger<FileDiscoveryService>.Instance);

        var result = await service.DiscoverAsync(AnalysisOptions.Default(Path.Combine(root, "App.slnx")), CancellationToken.None);

        Assert.Single(result.Projects);
        Assert.Equal(Path.Combine(root, "src", "App", "App.csproj"), result.Projects[0].ProjectPath);
        Assert.Single(result.AllFiles.Where(path => path.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task DiscoverAsync_applies_parent_and_child_configs_per_project_directory()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var root = temporaryDirectory.Path;
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        Directory.CreateDirectory(Path.Combine(root, "src", "Lib"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "globalusing.json"),
            """
            {
              "threshold": 90,
              "move": ["System.Text.Json"]
            }
            """);
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "App", "globalusing.json"),
            """
            {
              "threshold": 60,
              "globalFile": "AppGlobalUsings.cs"
            }
            """);
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "App", "App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "Program.cs"), "using System.Text.Json;");
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "Lib", "Lib.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Lib", "Library.cs"), "using System.Text.Json;");

        var service = new FileDiscoveryService(new PhysicalFileSystem(), NullLogger<FileDiscoveryService>.Instance);

        var result = await service.DiscoverAsync(AnalysisOptions.Default(root), CancellationToken.None);

        var appProject = Assert.Single(result.Projects.Where(project => project.ProjectPath == Path.Combine(root, "src", "App", "App.csproj")));
        var libProject = Assert.Single(result.Projects.Where(project => project.ProjectPath == Path.Combine(root, "src", "Lib", "Lib.csproj")));
        Assert.NotNull(appProject.EffectiveOptions);
        Assert.NotNull(libProject.EffectiveOptions);
        Assert.Equal(60, appProject.EffectiveOptions!.ThresholdPercentage);
        Assert.Equal("AppGlobalUsings.cs", appProject.EffectiveOptions.GlobalUsingsFileName);
        Assert.Equal(["System.Text.Json"], appProject.EffectiveOptions.MoveNamespaces);
        Assert.Equal(90, libProject.EffectiveOptions!.ThresholdPercentage);
        Assert.Equal("GlobalUsings.cs", libProject.EffectiveOptions.GlobalUsingsFileName);
        Assert.Equal(["System.Text.Json"], libProject.EffectiveOptions.MoveNamespaces);
    }
}
