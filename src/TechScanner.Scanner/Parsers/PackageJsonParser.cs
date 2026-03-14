using System.Text.Json;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class PackageJsonParser : IManifestParser
{
    private static readonly string[] DependencyKeys =
        ["dependencies", "devDependencies", "peerDependencies", "optionalDependencies"];

    public bool CanHandle(string fileName) =>
        Path.GetFileName(fileName).Equals("package.json", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(content); }
        catch { yield break; }

        using (doc)
        {
            foreach (var key in DependencyKeys)
            {
                if (!doc.RootElement.TryGetProperty(key, out var depsNode))
                    continue;

                foreach (var dep in depsNode.EnumerateObject())
                {
                    var name = dep.Name;
                    var version = dep.Value.GetString();
                    // Strip semver range operators
                    if (version != null)
                        version = version.TrimStart('^', '~', '>', '<', '=', ' ');

                    yield return new RawTechnology(name, version, filePath);
                }
            }
        }
    }
}
