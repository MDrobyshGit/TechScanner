using System.Text.RegularExpressions;

namespace TechScanner.Scanner.Analysis;

public class UsageAnalyzer
{
    private static readonly string[] SourceExtensions =
    [
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".java",
        ".kt", ".go", ".rs", ".rb", ".php"
    ];

    private const long MaxFileSizeBytes = 500 * 1024; // 500 KB

    public bool IsActiveInCode(string packageName, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(packageName) || packageName == "NEEDS_LLM_PARSE")
            return false;

        var normalizedName = NormalizePackageName(packageName);
        var pattern = BuildSearchPattern(normalizedName);

        foreach (var file in EnumerateSourceFiles(rootPath))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Length > MaxFileSizeBytes) continue;

                var content = File.ReadAllText(file);
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return false;
    }

    private static string NormalizePackageName(string name)
    {
        // Strip @scope/ prefix for npm packages like @angular/core → core (also search @angular)
        if (name.StartsWith('@'))
        {
            var slash = name.IndexOf('/');
            if (slash > 0) return name[(slash + 1)..];
        }
        // For Maven group:artifact → use artifact part
        var colon = name.IndexOf(':');
        if (colon > 0) return name[(colon + 1)..];

        return name;
    }

    private static string BuildSearchPattern(string name)
    {
        // Escape regex special chars
        var escaped = Regex.Escape(name);
        // Matches: import ... from 'name', require('name'), using Name, from name import, import name
        return $@"(from\s+['""].*{escaped}|require\s*\(\s*['""].*{escaped}|import\s+.*{escaped}|using\s+{escaped})";
    }

    private static IEnumerable<string> EnumerateSourceFiles(string rootPath)
    {
        return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(f => SourceExtensions.Contains(
                Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
    }
}
