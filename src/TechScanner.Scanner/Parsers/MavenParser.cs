using System.Xml.Linq;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class MavenParser : IManifestParser
{
    public bool CanHandle(string fileName) =>
        Path.GetFileName(fileName).Equals("pom.xml", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        XDocument doc;
        try { doc = XDocument.Parse(content); }
        catch { yield break; }

        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        foreach (var dependency in doc.Descendants(ns + "dependency"))
        {
            var groupId = dependency.Element(ns + "groupId")?.Value;
            var artifactId = dependency.Element(ns + "artifactId")?.Value;
            var version = dependency.Element(ns + "version")?.Value;

            if (string.IsNullOrWhiteSpace(artifactId)) continue;

            var name = string.IsNullOrWhiteSpace(groupId)
                ? artifactId
                : $"{groupId}:{artifactId}";

            // Skip property placeholders like ${project.version}
            if (version?.StartsWith("${") == true) version = null;

            yield return new RawTechnology(name, version, filePath);
        }
    }
}
