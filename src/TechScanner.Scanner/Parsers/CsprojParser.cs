using System.Xml.Linq;
using TechScanner.Core.Interfaces;
using TechScanner.Core.Models;

namespace TechScanner.Scanner.Parsers;

public class CsprojParser : IManifestParser
{
    public bool CanHandle(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || name.Equals("packages.config", StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<RawTechnology> Parse(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        XDocument doc;
        try { doc = XDocument.Parse(content); }
        catch { yield break; }

        // Modern SDK-style: <PackageReference Include="Name" Version="x.y.z" />
        foreach (var elem in doc.Descendants("PackageReference"))
        {
            var name = elem.Attribute("Include")?.Value;
            var version = elem.Attribute("Version")?.Value
                       ?? elem.Element("Version")?.Value;
            if (!string.IsNullOrWhiteSpace(name))
                yield return new RawTechnology(name, version, filePath);
        }

        // Legacy packages.config: <package id="Name" version="x.y.z" />
        foreach (var elem in doc.Descendants("package"))
        {
            var name = elem.Attribute("id")?.Value;
            var version = elem.Attribute("version")?.Value;
            if (!string.IsNullOrWhiteSpace(name))
                yield return new RawTechnology(name, version, filePath);
        }
    }
}
