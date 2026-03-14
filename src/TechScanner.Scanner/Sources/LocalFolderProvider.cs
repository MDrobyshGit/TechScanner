using TechScanner.Core.Interfaces;

namespace TechScanner.Scanner.Sources;

public class LocalFolderProvider : ISourceProvider
{
    public Task<string> PrepareAsync(string input, CancellationToken ct = default)
    {
        if (!Directory.Exists(input))
            throw new ArgumentException($"Directory does not exist: {input}");

        return Task.FromResult(input);
    }

    public void Cleanup(string tempPath)
    {
        // Local folder is not ours to clean up
    }
}
