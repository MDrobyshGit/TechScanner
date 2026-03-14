using System.Text.RegularExpressions;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class RequirementsTxtParser : IManifestParser
{
    private static readonly Regex PackageRegex =
        new(@"^([A-Za-z0-9_\-\.]+)([>=<!~^]+(.+))?$", RegexOptions.Compiled);

    public bool CanHandle(string fileName) =>
        Path.GetFileName(fileName).Equals("requirements.txt", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Strip inline comments
            var commentIdx = line.IndexOf('#');
            if (commentIdx > 0) line = line[..commentIdx].Trim();

            var match = PackageRegex.Match(line);
            if (!match.Success) continue;

            var name = match.Groups[1].Value;
            var version = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;
            yield return new RawTechnology(name, version, filePath);
        }
    }
}
