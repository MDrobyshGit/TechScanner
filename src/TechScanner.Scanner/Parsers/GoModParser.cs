using System.Text.RegularExpressions;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class GoModParser : IManifestParser
{
    private static readonly Regex RequireRegex = new(@"^\s*([^\s]+)\s+v([^\s]+)", RegexOptions.Compiled);

    public bool CanHandle(string fileName) =>
        Path.GetFileName(fileName).Equals("go.mod", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        bool inBlock = false;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("require (")) { inBlock = true; continue; }
            if (inBlock && line == ")") { inBlock = false; continue; }

            // Single-line require: require github.com/foo/bar v1.2.3
            if (line.StartsWith("require ") && !line.Contains('('))
            {
                var match = RequireRegex.Match(line["require ".Length..]);
                if (match.Success)
                    yield return new RawTechnology(match.Groups[1].Value, match.Groups[2].Value, filePath);
                continue;
            }

            if (inBlock)
            {
                var match = RequireRegex.Match(line);
                if (match.Success)
                    yield return new RawTechnology(match.Groups[1].Value, match.Groups[2].Value, filePath);
            }
        }
    }
}
