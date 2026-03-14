using TechScanner.Core.Interfaces;

namespace TechScanner.Scanner;

public class FileCollector
{
    private static readonly HashSet<string> IgnoredFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", ".git", "bin", "obj", ".gradle", "__pycache__",
        ".venv", "venv", "vendor", "dist", "build", ".idea", ".vs",
        "coverage", ".nyc_output", "target"
    };

    private readonly IEnumerable<IManifestParser> _parsers;

    public FileCollector(IEnumerable<IManifestParser> parsers)
    {
        _parsers = parsers;
    }

    public IEnumerable<string> Collect(string rootPath)
    {
        return CollectInternal(rootPath);
    }

    private IEnumerable<string> CollectInternal(string directory)
    {
        IEnumerable<string> files;
        try { files = Directory.GetFiles(directory); }
        catch (UnauthorizedAccessException) { yield break; }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            // Only return files that at least one non-fallback parser can handle
            if (_parsers.Any(p => p is not TechScanner.Scanner.Parsers.LlmFallbackParser && p.CanHandle(fileName)))
                yield return file;
        }

        IEnumerable<string> subdirs;
        try { subdirs = Directory.GetDirectories(directory); }
        catch (UnauthorizedAccessException) { yield break; }

        foreach (var subdir in subdirs)
        {
            var dirName = Path.GetFileName(subdir);
            if (IgnoredFolders.Contains(dirName)) continue;

            foreach (var file in CollectInternal(subdir))
                yield return file;
        }
    }
}
