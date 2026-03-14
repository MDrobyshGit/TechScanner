using System.Text.RegularExpressions;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class DockerfileParser : IManifestParser
{
    private static readonly Regex FromRegex = new(@"^FROM\s+([^\s:@]+)(?::([^\s@]+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ComposeImageRegex = new(@"^\s*image:\s*([^\s:]+)(?::([^\s]+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanHandle(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase)
            || name.Equals("dockerfile", StringComparison.OrdinalIgnoreCase)
            || name.Equals("docker-compose.yml", StringComparison.OrdinalIgnoreCase)
            || name.Equals("docker-compose.yaml", StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        var fileName = Path.GetFileName(filePath);
        bool isCompose = fileName.StartsWith("docker-compose", StringComparison.OrdinalIgnoreCase);

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;

            if (isCompose)
            {
                var match = ComposeImageRegex.Match(trimmed);
                if (match.Success)
                    yield return new RawTechnology(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, filePath);
            }
            else
            {
                var match = FromRegex.Match(trimmed);
                if (match.Success && !match.Groups[1].Value.Equals("scratch", StringComparison.OrdinalIgnoreCase))
                    yield return new RawTechnology(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, filePath);
            }
        }
    }
}
