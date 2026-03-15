namespace GlobalUsing.Core.Interfaces;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    void CreateDirectory(string path);

    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);

    Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken);
}
