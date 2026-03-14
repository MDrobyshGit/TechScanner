using System.Text.RegularExpressions;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class GradleParser : IManifestParser
{
    // Matches: implementation 'group:artifact:version'  or  implementation("group:artifact:version")
    private static readonly Regex GradleDepRegex = new(
        @"(?:implementation|api|compile|testImplementation|testCompile|runtimeOnly|compileOnly)\s*[\(""']([^""'()\s]+)[""'\)]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanHandle(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.Equals("build.gradle", StringComparison.OrdinalIgnoreCase)
            || name.Equals("build.gradle.kts", StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        foreach (Match match in GradleDepRegex.Matches(content))
        {
            var coord = match.Groups[1].Value; // group:artifact:version
            var parts = coord.Split(':');
            if (parts.Length < 2) continue;

            var name = parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : parts[0];
            var version = parts.Length >= 3 ? parts[2] : null;
            yield return new RawTechnology(name, version, filePath);
        }
    }
}
