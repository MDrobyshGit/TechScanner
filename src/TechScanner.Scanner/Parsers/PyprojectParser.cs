using System.Text.RegularExpressions;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class PyprojectParser : IManifestParser
{
    private static readonly Regex DepRegex =
        new(@"^([A-Za-z0-9_\-\.]+)\s*[>=<!~^]*(.*)$", RegexOptions.Compiled);

    public bool CanHandle(string fileName) =>
        Path.GetFileName(fileName).Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        bool inDeps = false;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            // Detect dependency sections
            if (line.StartsWith('['))
            {
                inDeps = line.Contains("dependencies", StringComparison.OrdinalIgnoreCase)
                      && !line.Contains("dev-dependencies", StringComparison.OrdinalIgnoreCase)
                      || line.Equals("[tool.poetry.dependencies]", StringComparison.OrdinalIgnoreCase)
                      || line.Equals("[project.dependencies]", StringComparison.OrdinalIgnoreCase)
                      || line.Equals("[tool.poetry.dev-dependencies]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inDeps || string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Format: name = "version" or name = {version = "..."}
            var eqIdx = line.IndexOf('=');
            if (eqIdx < 1) continue;

            var name = line[..eqIdx].Trim();
            if (name.Equals("python", StringComparison.OrdinalIgnoreCase)) continue;

            var valuePart = line[(eqIdx + 1)..].Trim().Trim('"', '\'');
            // Extract version from {version = "1.2.3"} inline table
            var versionMatch = Regex.Match(valuePart, @"version\s*=\s*[""']([^""']+)[""']");
            var version = versionMatch.Success
                ? versionMatch.Groups[1].Value
                : (valuePart.StartsWith('{') ? null : valuePart.TrimStart('^', '~', '>', '<', '='));

            yield return new RawTechnology(name, string.IsNullOrWhiteSpace(version) ? null : version, filePath);
        }
    }
}
