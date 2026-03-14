using TechScanner.Core.Models;

namespace TechScanner.Core.Interfaces;

public interface IManifestParser
{
    bool CanHandle(string fileName);
    IEnumerable<RawTechnology> Parse(string filePath, string content);
}
