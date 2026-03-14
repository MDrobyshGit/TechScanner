using System.IO.Compression;
using TechScanner.Core.Interfaces;

namespace TechScanner.Scanner.Sources;

public class ZipArchiveProvider : ISourceProvider
{
    public Task<string> PrepareAsync(string input, CancellationToken ct = default)
    {
        if (!File.Exists(input))
            throw new ArgumentException($"ZIP file does not exist: {input}");

        var tempDir = Path.Combine(Path.GetTempPath(), $"techscanner_{Guid.NewGuid()}");
        ZipFile.ExtractToDirectory(input, tempDir);
        return Task.FromResult(tempDir);
    }

    public void Cleanup(string tempPath)
    {
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, recursive: true);
    }
}
