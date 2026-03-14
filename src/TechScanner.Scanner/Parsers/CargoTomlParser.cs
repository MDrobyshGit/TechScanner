using System.Text.RegularExpressions;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class CargoTomlParser : IManifestParser
{
    private static readonly Regex SimpleDepRegex = new(@"^([A-Za-z0-9_\-]+)\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex TableDepRegex = new(@"^([A-Za-z0-9_\-]+)\s*=\s*\{[^}]*version\s*=\s*""([^""]+)""", RegexOptions.Compiled);

    public bool CanHandle(string fileName) =>
        Path.GetFileName(fileName).Equals("Cargo.toml", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        bool inDeps = false;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith('['))
            {
                inDeps = line.Contains("dependencies", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inDeps || string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var tableMatch = TableDepRegex.Match(line);
            if (tableMatch.Success)
            {
                yield return new RawTechnology(tableMatch.Groups[1].Value, tableMatch.Groups[2].Value, filePath);
                continue;
            }

            var simpleMatch = SimpleDepRegex.Match(line);
            if (simpleMatch.Success)
                yield return new RawTechnology(simpleMatch.Groups[1].Value, simpleMatch.Groups[2].Value, filePath);
        }
    }
}
