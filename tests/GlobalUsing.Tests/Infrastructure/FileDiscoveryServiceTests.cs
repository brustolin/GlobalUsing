
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

        result.AllFiles.Should().ContainSingle().Which.Should().Be(Path.Combine(root, "Program.cs"));
        result.Projects.Should().ContainSingle();
        result.Projects[0].ImplicitUsingsEnabled.Should().BeTrue();
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

        result.Projects.Should().ContainSingle();
        result.Projects[0].ProjectPath.Should().Be(Path.Combine(root, "src", "App", "App.csproj"));
        result.AllFiles.Should().ContainSingle(path => path.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase));
    }
}
